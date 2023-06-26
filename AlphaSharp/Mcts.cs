using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AlphaSharp.Interfaces;
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

        public static long TicksWaited = 0;
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

                Interlocked.Add(ref Mcts.TicksWaited, ticks);
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
        private ConcurrentDictionary<int, ThreadData> _threadDic = new ConcurrentDictionary<int, ThreadData>();

        public int NumberOfCachedStates => _cachedStateLookup.Count;
        private readonly Dictionary<string, int> _cachedStateLookup = new();

        public Mcts(IGame game, ISkynet skynet, AlphaParameters args)
        {
            TicksWaited = 0;
            _game = game;
            _skynet = skynet;
            _param = args;
        }
        
        public float[] GetActionPolicy(byte[] state, int playerTurn, float simulationDecay)
            => GetActionPolicyInternal(state, playerTurn, isSelfPlay: false, simulationDecay);

        public float[] GetActionPolicyForSelfPlay(byte[] state, int playerTurn, float simulationDecay, float temperature)
            => GetActionPolicyInternal(state, playerTurn, isSelfPlay: true, simulationDecay, temperature);

        private float[] GetActionPolicyInternal(byte[] state, int playerTurn, bool isSelfPlay, float simulationDecay, float temperature = 0.0f)
        {
            int ExploreThreadMain(int _)
            {
                ExploreGameTree(state, isSimulation: isSelfPlay, playerTurn);
                return 0;
            }

            _threadDic.Clear();

            int iterations = (int)((isSelfPlay ? _param.SelfPlaySimulationIterations : _param.EvalSimulationIterations) * simulationDecay);
            var workList = Enumerable.Range(0, iterations).ToList();
            var threadedSimulation = new ThreadedWorker<int, int>(ExploreThreadMain, workList, _param.MaxWorkerThreads);
            threadedSimulation.Run();

            //_param.TextInfoCallback(LogLevel.Info, $"mcts has {_cachedStateLookup.Count} cached states");

            var cachedState = GetOrCreateLockedCachedState(state, out bool wasCreated);
            cachedState.Unlock();

            if (wasCreated)
                throw new ArgumentException("BUG: after exploring this state should have been cached");

            var policy = new float[cachedState.Actions.Length];

            if (isSelfPlay)
            {
                var threadData = GetThreadForCurrentThread();
                for (int i = 0; i < threadData.ActionsVisitCountReused.Length; i++)
                {
                    policy[i] = cachedState.Actions[i].VisitCount;
                }

                Util.Softmax(policy, temperature);

                return policy;
            }
            else
            {
                int selectedAction = ActionUtil.PickActionByHighestVisitCount(cachedState.Actions);
                policy[selectedAction] = 1.0f;

                return policy;
            }
        }

        private ThreadData GetThreadForCurrentThread()
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
            var threadData = GetThreadForCurrentThread();

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

                if (IsGameOver(cachedState, isSimulation, threadData, out float gameOverV))
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

        private bool IsGameOver(CachedState cachedState, bool isSimulation, ThreadData threadData, out float v)
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

            cachedState.GameOver = gameOverStatus;
            cachedState.VisitCount++;

            // moves are always made as player1. the latest added actions
            // belongs to current player, no matter if this is pl1 or pl2.
            // so we want score from p1 perspective. It cannot just be 1 since
            // in some games the player might be able to make a move that
            // loses the game.
            v = GameOver.ValueForPlayer1(cachedState.GameOver);
            return true;
        }

        private int RevisitNode(CachedState cachedState, bool isSimulation, ThreadData threadData)
        {
            //_param.TextInfoCallback(LogLevel.Verbose, $"revisiting a cached state with visitCount {cachedState.VisitCount}, nodeIdx: {cachedState.Idx}, player: {player}, moves: {_selectedActions.Count}");

            // Pick action with highest UCB
            float bestUpperConfidence = float.NegativeInfinity;
            int selectedAction = -1;

            bool isFirstMove = threadData.SelectedActions.Count == 0;

            bool addNoise = isFirstMove && isSimulation;
            if (addNoise)
            {
                Noise.CreateDirichlet(threadData.NoiseReused, _param.DirichletNoiseShape);
            }
            else
            {
                Array.Clear(threadData.NoiseReused, 0, threadData.NoiseReused.Length);
            }

            // Increase state visit count before calculating U, this way actionProbability will be dominant until there is at least one action visitcount
            cachedState.VisitCount++;

            for (int i = 0; i < cachedState.Actions.Length; i++)
            {
                ref Action action = ref cachedState.Actions[i];
                if (action.IsValidMove != 0)
                {
                    float actionProbability = addNoise ? action.ActionProbability + threadData.NoiseReused[i] * _param.DirichletNoiseScale : action.ActionProbability;

                    float u = action.Q + _param.Cpuct * actionProbability * (float)Math.Sqrt(cachedState.VisitCount) / (1.0f + action.VisitCount);

                    //float u = action.Q + actionProbability * cachedState.VisitCount / (action.VisitCount + 1) * _param.Cpuct / (float)Math.Sqrt(action.VisitCount + 1);
                    //_param.TextInfoCallback(LogLevel.Verbose, $"considering action: {i}, nodeIdx: {cachedState.Idx}, visitcount: {action.VisitCount}, q: {action.Q}, ucb: {u}, prob: {action.ActionProbability}, player: {player}, moves: {_selectedActions.Count}, noise: {action.ActionProbability} -> {_noiseReused[i]} = {actionProbability}");

                    u -= action.VirtualLoss * 0.5f;
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
            _skynet.Suggest(threadData.CurrentState, threadData.ActionProbsReused, out float v);
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

                action.VisitCount++;
                action.VirtualLoss--;
                if (action.VirtualLoss < 0)
                {
                    throw new Exception("Virtual loss should never be negative");
                }

                //float oldQ = action.Q;
                action.Q = (action.VisitCount * action.Q + v) / (action.VisitCount + 1);

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
            _mctsSpinLock.Enter(ref lockTaken);

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
