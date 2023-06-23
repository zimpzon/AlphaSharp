using System;
using System.Collections.Generic;
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
            public float ActionProbability { get; set; }

            public override readonly string ToString()
                => $"Q: {Q}, VisitCount: {VisitCount}, IsValidMove: {IsValidMove}, ActionProbability: {ActionProbability}";
        }

        private sealed class SelectedAction
        {
            public int NodeIdx { get; set; }
            public int ActionIdx { get; set; }
            public float Player { get; set; }
            public override string ToString()
                => $"NodeIdx: {NodeIdx}, ActionIdx: {ActionIdx}, Player: {Player}";
        }

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
                bool lockTaken = false;
                SpinLock.Enter(ref lockTaken);
                if (!lockTaken)
                    throw new Exception("Failed to lock state");
            }

            public void Unlock()
            {
                SpinLock.Exit(useMemoryBarrier: true);
            }
        }

        private readonly IGame _game;
        private readonly ISkynet _skynet;
        private readonly AlphaParameters _param;
        private CachedState[] _cachedStates = new CachedState[200000];
        private int _stateIdx = 0;
        private readonly float[] _actionProbsReused; // per thread
        private readonly float[] _noiseReused;
        private readonly double[] _actionsVisitCountReused; // per thread
        private readonly byte[] _validActionsReused; // per thread
        private readonly List<SelectedAction> _selectedActions = new(); // per thread
        private readonly byte[] _currentState; // per thread
        private SpinLock _mctsSpinLock = new();
        private const int NoValidAction = -1;

        private readonly Dictionary<string, int> _cachedStateLookup = new();

        public Mcts(IGame game, ISkynet skynet, AlphaParameters args)
        {
            _game = game;
            _skynet = skynet;
            _param = args;

            _actionProbsReused = new float[_game.ActionCount];
            _noiseReused = new float[_game.ActionCount];
            _validActionsReused = new byte[_game.ActionCount];
            _actionsVisitCountReused = new double[_game.ActionCount];
            _currentState = new byte[_game.StateSize];
        }
        
        public float[] GetActionPolicy(byte[] state, int playerTurn)
            => GetActionPolicyInternal(state, playerTurn, isSelfPlay: false);

        public float[] GetActionPolicyForSelfPlay(byte[] state, int playerTurn, float temperature)
            => GetActionPolicyInternal(state, playerTurn, isSelfPlay: true, temperature);

        private float[] GetActionPolicyInternal(byte[] state, int playerTurn, bool isSelfPlay, float temperature = 0.0f)
        {
            int simCount = _param.SimulationIterations;

            for (int i = 0; i < simCount; i++)
            {
                // threading here
                ExploreGameTree(state, isSimulation: isSelfPlay, playerTurn, i, simCount);
            }

            var cachedState = GetOrCreateLockedCachedState(state, out bool wasCreated);
            cachedState.Unlock();

            if (wasCreated)
                throw new ArgumentException("BUG: after exploring this state should have been cached");

            var policy = new float[cachedState.Actions.Length];

            if (isSelfPlay)
            {
                for (int i = 0; i < _actionsVisitCountReused.Length; i++)
                {
                    policy[i] = cachedState.Actions[i].VisitCount;
                }

                ArrayUtil.Softmax(policy, temperature);

                return policy;
            }
            else
            {
                int selectedAction = ActionUtil.PickActionByHighestVisitCount(cachedState.Actions);
                policy[selectedAction] = 1.0f;

                return policy;
            }
        }

        private void ExploreGameTree(byte[] startingState, bool isSimulation, int playerTurn, int simNo, int simCount)
        {
            Array.Copy(startingState, _currentState, _currentState.Length);
            _selectedActions.Clear();

            //lock (_mctsLock)
            //    _param.TextInfoCallback(LogLevel.Verbose, $"--- starting simulation from root as player {playerTurn} ({simNo + 1}/{simCount}) ---");

            while (true)
            {
                var cachedState = GetOrCreateLockedCachedState(_currentState, out bool wasCreated);

                bool revisitedGameOver = cachedState.GameOver != GameOver.Status.GameIsNotOver;
                if (revisitedGameOver)
                {
                    cachedState.VisitCount++;

                    // This may repeat many times at the end of a simulation loop since the remaining simulations will just exploit this known winning state over and over.
                    //_param.TextInfoCallback(LogLevel.Verbose, $"revisiting a cached game over: {cachedState.GameOver}, nodeIdx: {cachedState.Idx}, player: {playerTurn}, moves: {_selectedActions.Count}");

                    // the latest move made was by the opponent, so they should receive a negative reward for moving
                    cachedState.Unlock();
                    BacktrackAndUpdate(_selectedActions, -1, currentPlayer: playerTurn);
                    break;
                }

                if (wasCreated)
                {
                    float expandV = ExpandState(cachedState, playerTurn);

                    // latest recorded action was the opponents, but v is for me, so negate v
                    cachedState.Unlock();
                    BacktrackAndUpdate(_selectedActions, -expandV, currentPlayer: playerTurn);
                    break;
                }

                int selectedAction = RevisitNode(cachedState, isSimulation, playerTurn);

                //Console.WriteLine($"before move ({playerTurn}) :");
                //_game.PrintState(_currentState, Console.WriteLine);

                if (selectedAction != NoValidAction)
                {
                    _game.ExecutePlayerAction(_currentState, selectedAction);
                }

                //Console.WriteLine($"after move ({playerTurn}) :");
                //_game.PrintState(_currentState, Console.WriteLine);

                if (IsGameOver(cachedState, isSimulation, playerTurn, out float gameOverV))
                {
                    cachedState.Unlock();
                    BacktrackAndUpdate(_selectedActions, gameOverV, currentPlayer: playerTurn);
                    break;
                }

                cachedState.Unlock();

                _game.FlipStateToNextPlayer(_currentState);

                playerTurn = -playerTurn;
                //_param.TextInfoCallback(LogLevel.Verbose, $"player switched from {-playerTurn} to {playerTurn}, nodeIdx: {cachedState.Idx}, player: {playerTurn}, moves: {_selectedActions.Count}");
            }
        }

        private bool IsGameOver(CachedState cachedState, bool isSimulation, int player, out float v)
        {
            var gameOverStatus = _game.GetGameEnded(_currentState, _selectedActions.Count, isSimulation);
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

        private int RevisitNode(CachedState cachedState, bool isSimulation, int player)
        {
            _param.TextInfoCallback(LogLevel.Verbose, $"revisiting a cached state with visitCount {cachedState.VisitCount}, nodeIdx: {cachedState.Idx}, player: {player}, moves: {_selectedActions.Count}");

            // Pick action with highest UCB
            float bestUpperConfidence = float.NegativeInfinity;
            int selectedAction = -1;

            bool isFirstMove = _selectedActions.Count == 0;

            bool addNoise = isFirstMove && isSimulation;
            if (addNoise)
            {
                Noise.CreateDirichlet(_noiseReused, _param.DirichletNoiseShape);
            }
            else
            {
                Array.Clear(_noiseReused, 0, _noiseReused.Length);
            }

            // Increase state visit count before calculating U, this way actionProbability will be dominant until there is at least one action visitcount
            cachedState.VisitCount++;

            for (int i = 0; i < cachedState.Actions.Length; i++)
            {
                ref Action action = ref cachedState.Actions[i];
                if (action.IsValidMove != 0)
                {
                    float actionProbability = addNoise ? action.ActionProbability + _noiseReused[i] * _param.DirichletNoiseScale : action.ActionProbability;

                    //float u = action.Q + _param.Cpuct * actionProbability * (float)Math.Sqrt(cachedState.VisitCount) / (1.0f + action.VisitCount);

                    float u = action.Q + actionProbability * cachedState.VisitCount / (action.VisitCount + 1) * _param.Cpuct / (float)Math.Sqrt(action.VisitCount + 1);
                    _param.TextInfoCallback(LogLevel.Verbose, $"considering action: {i}, nodeIdx: {cachedState.Idx}, visitcount: {action.VisitCount}, q: {action.Q}, ucb: {u}, prob: {action.ActionProbability}, player: {player}, moves: {_selectedActions.Count}, noise: {action.ActionProbability} -> {_noiseReused[i]} = {actionProbability}");

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
                _selectedActions.Add(new SelectedAction { NodeIdx = cachedState.Idx, ActionIdx = selectedAction, Player = player });

                _param.TextInfoCallback(LogLevel.Verbose, $"action selected: {selectedAction}, nodeIdx: {cachedState.Idx}, confidence: {bestUpperConfidence}, player: {player}, moves: {_selectedActions.Count}");
            }

            return selectedAction;
        }

        private float ExpandState(CachedState cachedState, int playerTurn)
        {
            // get and save suggestions from Skynet, then backtrack to root using suggested v
            _skynet.Suggest(_currentState, _actionProbsReused, out float v);
            _game.GetValidActions(_currentState, _validActionsReused);

            ArrayUtil.FilterProbsByValidActions(_actionProbsReused, _validActionsReused);

            //_param.TextInfoCallback(LogLevel.Verbose, $"new state node created, nodeIdx: {cachedState.Idx}, network v: {v}, player: {playerTurn}, moves: {_selectedActions.Count}");

            bool hasValidActions = ArrayUtil.CountNonZero(_actionProbsReused) > 0;
            if (!hasValidActions)
            {
                // no valid actions in leaf, consider this a draw, ex TicTacToe: board is full
                //_param.TextInfoCallback(LogLevel.Verbose, $"no valid actions in new state node, nodeIdx: {cachedState.Idx}, player: {playerTurn}, moves: {_selectedActions.Count}");
                return 0;
            }

            ArrayUtil.Normalize(_actionProbsReused);

            for (int i = 0; i < cachedState.Actions.Length; ++i)
            {
                cachedState.Actions[i].ActionProbability = _actionProbsReused[i];
                cachedState.Actions[i].IsValidMove = _validActionsReused[i];
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
