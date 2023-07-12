using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AlphaSharp.Interfaces;
using static AlphaSharp.Mcts;
using Math = System.Math;

namespace AlphaSharp
{
    public class Mcts
    {
        public struct Action
        {
            public float Q { get; set; }
            public int VisitCount { get; set; }
            public byte IsValidMove { get; set; }
            public byte VirtualLoss { get; set; }
            public float ActionProbability { get; set; }

            public override readonly string ToString()
                => $"Q: {Q}, VisitCount: {VisitCount}, IsValidMove: {IsValidMove}, ActionProbability: {ActionProbability}";
        }

        private sealed class SelectedAction
        {
            public int NodeIdx { get; set; }
            public int ActionIdx { get; set; }
            public override string ToString()
                => $"NodeIdx: {NodeIdx}, ActionIdx: {ActionIdx}";
        }

        public static long TicksBlockedAccessNode = 0;
        public static long TicksBlockedGetOrCreateNode = 0;
        public static long TicksInference = 0;

        public class CachedState
        {
            public CachedState(int actionCount, int idx)
            {
                Actions = new Action[actionCount];
                Idx = idx;
            }

            public int Idx { get; set; }
            public long VisitCount;
            public SpinLock SpinLock = new();
            public GameOver.Status GameOver { get; set; }
            public Action[] Actions { get; set; }
            
            public void Lock()
            {
                var sw = Stopwatch.StartNew();
                bool lockTaken = false;
                SpinLock.Enter(ref lockTaken);
                if (!lockTaken)
                    throw new Exception("Failed to lock state");

                long ticks = sw.Elapsed.Ticks;
                Interlocked.Add(ref TicksBlockedAccessNode, ticks);
            }

            public void Unlock()
            {
                SpinLock.Exit(useMemoryBarrier: true);
            }
        }

        private sealed class ThreadData
        {
            public ThreadData(int actionCount, int stateSize)
            {
                ActionProbsReused = new float[actionCount];
                NoiseReused = new float[actionCount];
                ValidActionsReused = new byte[actionCount];
                ActionsVisitCountReused = new double[actionCount];
                CurrentState = new byte[stateSize];
            }

            public readonly float[] ActionProbsReused; // per thread
            public readonly float[] NoiseReused;
            public readonly double[] ActionsVisitCountReused; // per thread
            public readonly byte[] ValidActionsReused; // per thread
            public readonly List<SelectedAction> SelectedActions = new(); // per thread
            public readonly byte[] CurrentState; // per thread
        }

        private readonly IGame _game;
        private readonly ISkynet _skynet;
        private readonly AlphaParameters _param;
        private CachedState[] _cachedStates = new CachedState[200000];
        private int _stateIdx = 0;
        private SpinLock _mctsSpinLock = new();
        private const int NoValidAction = -1;
        private bool _isSleepCycle;

        private readonly ConcurrentDictionary<int, ThreadData> _threadDic = new ConcurrentDictionary<int, ThreadData>();

        public int NumberOfCachedStates => _cachedStateLookup.Count;
        private readonly Dictionary<string, int> _cachedStateLookup = new();

        public Mcts(IGame game, ISkynet skynet, AlphaParameters args)
        {
            TicksBlockedAccessNode = 0;
            TicksBlockedGetOrCreateNode = 0;
            TicksInference = 0;

            _game = game;
            _skynet = skynet;
            _param = args;
        }

        public float[] GetActionPolicy(byte[] state, int playerTurn)
            => GetActionPolicyInternal(state, playerTurn, isSelfPlay: false, isSleepCycle: false);

        public float[] GetActionPolicyForSelfPlay(byte[] state, int playerTurn, bool isSleepCycle, float temperature)
            => GetActionPolicyInternal(state, playerTurn, isSelfPlay: true, isSleepCycle, temperature);

        private float[] GetActionPolicyInternal(byte[] state, int playerTurn, bool isSelfPlay, bool isSleepCycle, float temperature = 1.0f)
        {
            _isSleepCycle = isSleepCycle;

            int ExploreThreadMain(int _)
            {
                ExploreGameTree(state, isSimulation: isSelfPlay, playerTurn);
                return 0;
            }

            _threadDic.Clear();

            int iterations = isSelfPlay ? _param.SelfPlaySimulationIterations : _param.EvalSimulationIterations;
            if (isSleepCycle)
                iterations *= 2;

            var workList = Enumerable.Range(0, iterations).ToList();
            var threadedSimulation = new ThreadedWorker<int, int>(ExploreThreadMain, workList, isSelfPlay ? _param.MaxWorkerThreads : Math.Max(1,  _param.MaxWorkerThreads / 2));
            threadedSimulation.Run();

            //_param.TextInfoCallback(LogLevel.Info, $"mcts has {_cachedStateLookup.Count} cached states");

            var cachedState = GetOrCreateLockedCachedState(state, out bool wasCreated);
            cachedState.Unlock();

            if (wasCreated)
                throw new ArgumentException("BUG: after exploring this state should have been cached");

            var policy = new float[cachedState.Actions.Length];

            var threadData = GetThreadDataForCurrentThread();
            for (int i = 0; i < threadData.ActionsVisitCountReused.Length; i++)
            {
                // Use prob if no visitcounts - if we didn't traverse tree we only have prob values
                policy[i] = cachedState.Actions[i].VisitCount != 0 ? cachedState.Actions[i].VisitCount : cachedState.Actions[i].ActionProbability + 0.0001f;
            }

            Util.Softmax(policy, temperature);

            return policy;
        }

        private ThreadData GetThreadDataForCurrentThread()
        {
            if (!_threadDic.TryGetValue(Environment.CurrentManagedThreadId, out ThreadData threadData))
            {
                threadData = new ThreadData(_game.ActionCount, _game.StateSize);
                _threadDic[Environment.CurrentManagedThreadId] = threadData;
            }

            return threadData;
        }

        private void ExploreGameTree(byte[] startingState, bool isSimulation, int playerTurn)
        {
            var threadData = GetThreadDataForCurrentThread();

            Array.Copy(startingState, threadData.CurrentState, threadData.CurrentState.Length);
            threadData.SelectedActions.Clear();

            //Console.WriteLine($"starting simulation from root as player {playerTurn} ({Environment.CurrentManagedThreadId})");

            //lock (_mctsLock)
            //    _param.TextInfoCallback(LogLevel.Verbose, $"--- starting simulation from root as player {playerTurn} ({simNo + 1}/{simCount}) ---");

            while (true)
            {
                var cachedState = GetOrCreateLockedCachedState(threadData.CurrentState, out bool wasCreated);

                bool revisitedGameOver = cachedState.GameOver != GameOver.Status.GameIsNotOver;
                if (revisitedGameOver)
                {
                    cachedState.VisitCount++;

                    // This may repeat many times at the end of a simulation loop since the remaining simulations will just exploit this known winning state over and over.
                    //_param.TextInfoCallback(LogLevel.Verbose, $"revisiting a cached game over: {cachedState.GameOver}, nodeIdx: {cachedState.Idx}, player: {playerTurn}, moves: {_selectedActions.Count}");

                    // the latest move made was by the opponent, so they should receive a negative reward for moving
                    cachedState.Unlock();
                    BacktrackAndUpdate(threadData.SelectedActions, -1, currentPlayer: playerTurn);
                    break;
                }

                if (wasCreated)
                {
                    float expandV = ExpandState(cachedState, threadData);

                    // latest recorded action was the opponents, but v is for me, so negate v
                    cachedState.Unlock();
                    BacktrackAndUpdate(threadData.SelectedActions, -expandV, currentPlayer: playerTurn);
                    break;
                }

                int selectedAction = RevisitNode(cachedState, isSimulation, threadData);

                //Console.WriteLine($"before move ({playerTurn}) :");
                //_game.PrintState(_currentState, Console.WriteLine);

                if (selectedAction != NoValidAction)
                {
                    _game.ExecutePlayerAction(threadData.CurrentState, selectedAction);
                }

                //Console.WriteLine($"after move ({playerTurn}) :");
                //_game.PrintState(_currentState, Console.WriteLine);

                if (IsGameOver(isSimulation, threadData, out float gameOverV))
                {
                    cachedState.Unlock();
                    BacktrackAndUpdate(threadData.SelectedActions, gameOverV, currentPlayer: playerTurn);
                    break;
                }

                cachedState.Unlock();

                _game.FlipStateToNextPlayer(threadData.CurrentState);

                playerTurn = -playerTurn;
                //_param.TextInfoCallback(LogLevel.Verbose, $"player switched from {-playerTurn} to {playerTurn}, nodeIdx: {cachedState.Idx}, player: {playerTurn}, moves: {_selectedActions.Count}");
            }
        }

        private bool IsGameOver(bool isSimulation, ThreadData threadData, out float v)
        {
            var gameOverStatus = _game.GetGameEnded(threadData.CurrentState, threadData.SelectedActions.Count, isSimulation);
            if (gameOverStatus == GameOver.Status.GameIsNotOver)
            {
                v = 0;
                return false;
            }

            if (gameOverStatus == GameOver.Status.DrawDueToMaxMovesReached)
            {
                // do not mark this state as a draw. It is not the state itself that is game over, we just ran out of moves.
                //_param.TextInfoCallback(LogLevel.Verbose, $"max moves reached, game over: {gameOverStatus}, nodeIdx: {cachedState.Idx}, player: {player}, moves: {_selectedActions.Count}");
                v = 0;
                return true;
            }

            var winState = GetOrCreateLockedCachedState(threadData.CurrentState, out bool _);
            winState.GameOver = gameOverStatus;
            winState.VisitCount++;

            // moves are always made as player1. the latest added actions
            // belongs to current player, no matter if this is pl1 or pl2.
            // so we want score from p1 perspective. It cannot just be 1 since
            // in some games the player might be able to make a move that
            // loses the game.
            v = GameOver.ValueForPlayer1(winState.GameOver);
            winState.Unlock();
            return true;
        }

        private int RevisitNode(CachedState cachedState, bool isSimulation, ThreadData threadData)
        {
            //_param.TextInfoCallback(LogLevel.Verbose, $"revisiting a cached state with visitCount {cachedState.VisitCount}, nodeIdx: {cachedState.Idx}, player: {player}, moves: {_selectedActions.Count}");

            // Pick action with highest UCB
            float bestUpperConfidence = float.NegativeInfinity;
            int selectedAction = -1;

            bool isFirstMove = threadData.SelectedActions.Count == 0;

            float rnd = (float)Random.Shared.NextDouble();

            bool addNoise = (isFirstMove && isSimulation) || (_isSleepCycle && _param.SelfPlaySleepNoiseChance > rnd);
            if (addNoise)
            {
                Noise.CreateDirichlet(threadData.NoiseReused, _param.DirichletNoiseShape);
            }

            // Increase state visit count before calculating U, this way actionProbability will be dominant until there is at least one action visitcount
            cachedState.VisitCount++;

            for (int i = 0; i < cachedState.Actions.Length; i++)
            {
                ref Action action = ref cachedState.Actions[i];
                if (action.IsValidMove != 0)
                {
                    float noiseScale = _param.DirichletNoiseScale;
                    float noiseValue = threadData.NoiseReused[i];

                    float actionProbability = addNoise ? action.ActionProbability + noiseValue * noiseScale : action.ActionProbability;

                    // TODO: should we do anything about Q = 0 when action has not been visited?
                    // https://github.com/suragnair/alpha-zero-general/discussions/72
                   
                    float u = action.Q + _param.Cpuct * actionProbability * (float)Math.Sqrt(cachedState.VisitCount) / (1.0f + action.VisitCount);

                    //float u = action.Q + actionProbability * cachedState.VisitCount / (action.VisitCount + 1) * _param.Cpuct / (float)Math.Sqrt(action.VisitCount + 1);
                    //_param.TextInfoCallback(LogLevel.Verbose, $"considering action: {i}, nodeIdx: {cachedState.Idx}, visitcount: {action.VisitCount}, q: {action.Q}, ucb: {u}, prob: {action.ActionProbability}, player: {player}, moves: {_selectedActions.Count}, noise: {action.ActionProbability} -> {_noiseReused[i]} = {actionProbability}");

                    u -= action.VirtualLoss;
                    if (u > bestUpperConfidence)
                    {
                        bestUpperConfidence = u;
                        selectedAction = i;
                    }
                }
            }

            if (selectedAction != NoValidAction)
            {
                // an action was selected
                threadData.SelectedActions.Add(new SelectedAction { NodeIdx = cachedState.Idx, ActionIdx = selectedAction });

                ref Action action = ref cachedState.Actions[selectedAction];
                action.VirtualLoss++;

                //_param.TextInfoCallback(LogLevel.Verbose, $"action selected: {selectedAction}, nodeIdx: {cachedState.Idx}, confidence: {bestUpperConfidence}, player: {player}, moves: {_selectedActions.Count}");
            }

            return selectedAction;
        }

        private float ExpandState(CachedState cachedState, ThreadData threadData)
        {
            // get and save suggestions from Skynet, then backtrack to root using suggested v

            var sw = Stopwatch.StartNew();

            _skynet.Suggest(threadData.CurrentState, threadData.ActionProbsReused, out float v);

            long ticks = sw.Elapsed.Ticks;
            Interlocked.Add(ref TicksInference, ticks);

            // don't allow 0 probs, it might cause 0 valid actions in worst case
            Util.Add(threadData.ActionProbsReused, 0.00001f);

            _game.GetValidActions(threadData.CurrentState, threadData.ValidActionsReused);
            Util.FilterProbsByValidActions(threadData.ActionProbsReused, threadData.ValidActionsReused);

            //_param.TextInfoCallback(LogLevel.Verbose, $"new state node created, nodeIdx: {cachedState.Idx}, network v: {v}, player: {playerTurn}, moves: {_selectedActions.Count}");

            bool hasValidActions = Util.CountNonZero(threadData.ActionProbsReused) > 0;
            if (!hasValidActions)
            {
                // no valid actions in leaf, consider this a draw, ex TicTacToe: board is full
                //_param.TextInfoCallback(LogLevel.Verbose, $"no valid actions in new state node, nodeIdx: {cachedState.Idx}, player: {playerTurn}, moves: {_selectedActions.Count}");
                return 0;
            }

            Util.Normalize(threadData.ActionProbsReused);

            for (int i = 0; i < cachedState.Actions.Length; ++i)
            {
                cachedState.Actions[i].ActionProbability = threadData.ActionProbsReused[i];
                cachedState.Actions[i].IsValidMove = threadData.ValidActionsReused[i];
            }

            cachedState.VisitCount = 1;

            return v;
        }

        private void BacktrackAndUpdate(List<SelectedAction> selectedActions, float v, float currentPlayer)
        {
            //_param.TextInfoCallback(LogLevel.Verbose, $"backtracking sim result, v: {v}, moves/actions selected: {_selectedActions.Count}, initiated by player: {currentPlayer}");
            for (int i = selectedActions.Count - 1; i >= 0; --i)
            {
                currentPlayer = -currentPlayer;

                var node = GetLockedCachedStateByIndex(selectedActions[i].NodeIdx);

                int a = selectedActions[i].ActionIdx;
                ref var action = ref node.Actions[a];

                action.VirtualLoss--;

                if (action.VirtualLoss < 0)
                {
                    throw new Exception("Virtual loss should never be negative");
                }

                //float oldQ = action.Q;
                action.Q = (action.VisitCount * action.Q + v) / (action.VisitCount + 1);
                action.VisitCount++;

                //_param.TextInfoCallback(LogLevel.Verbose, $"updating nodeIdx {node.Idx}, actionIdx: {a}, v: {v}, oldQ: {oldQ}, newQ: {action.Q}, action visitCount: {action.VisitCount}, action taken by player: {selectedActions[i].Player}");

                // switch to the other players perspective
                v = -v;

                node.Unlock();
            }
        }

        private CachedState GetLockedCachedStateByIndex(int idx)
        {
            bool lockTaken = false;
            _mctsSpinLock.Enter(ref lockTaken);

            var result = _cachedStates[idx];
            _mctsSpinLock.Exit();

            result.Lock();
            return result;
        }

        private CachedState GetOrCreateLockedCachedState(byte[] state, out bool wasCreated)
        {
            bool lockTaken = false;

            var sw = Stopwatch.StartNew();
            _mctsSpinLock.Enter(ref lockTaken);
            long ticks = sw.Elapsed.Ticks;
            Interlocked.Add(ref TicksBlockedGetOrCreateNode, ticks);

            string key = Convert.ToBase64String(state);
            if (_cachedStateLookup.TryGetValue(key, out int idx))
            {
                wasCreated = false;

                var existing = _cachedStates[idx];
                _mctsSpinLock.Exit();

                existing.Lock();
                return existing;
            }

            if (_stateIdx >= _cachedStates.Length)
            {
                int newSize = (int)(_cachedStates.Length * 1.5);
                _param.TextInfoCallback(LogLevel.MoreInfo, $"expanding cachedStates array from {_cachedStates.Length} to {newSize}");

                Array.Resize(ref _cachedStates, newSize);
            }

            _cachedStates[_stateIdx] = new CachedState(_game.ActionCount, _stateIdx);
            _cachedStateLookup.Add(key, _stateIdx);

            _stateIdx++;
            wasCreated = true;

            var result = _cachedStates[_stateIdx - 1];
            _mctsSpinLock.Exit();

            result.Lock();
            return result;
        }
    }
}
