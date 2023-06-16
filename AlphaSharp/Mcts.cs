using System;
using System.Collections.Generic;
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
        }

        public class CachedState
        {
            public CachedState(int actionCount, int idx)
            {
                Actions = new Action[actionCount];
                Idx = idx;
            }

            public int Idx { get; set; }
            public int VisitCount { get; set; }
            public GameOver.Status GameOver { get; set; }
            public Action[] Actions { get; set; }
        }

        private readonly IGame _game;
        private readonly ISkynet _skynet;
        private readonly AlphaParameters _param;
        private CachedState[] _cachedStates = new CachedState[200000];
        private int _stateIdx = 0;
        private readonly float[] _actionProbsReused;
        private readonly float[] _noiseReused;
        private readonly double[] _actionsVisitCountReused;
        private readonly byte[] _validActionsReused;
        private readonly List<SelectedAction> _selectedActions = new();
        private readonly byte[] _currentState;

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
        
        public float[] GetActionPolicy(byte[] state)
            => GetActionPolicyInternal(state, isSelfPlay: false);

        public float[] GetActionPolicyForSelfPlay(byte[] state, float temperature)
            => GetActionPolicyInternal(state, isSelfPlay: true, temperature);

        private float[] GetActionPolicyInternal(byte[] state, bool isSelfPlay, float temperature = 0.0f)
        {
            int simCount = _param.SimulationIterations;

            for (int i = 0; i < simCount; i++)
            {
                ExploreGameTree(state, isSimulation: isSelfPlay);
            }

            int nodeIdx = GetOrCreateCachedState(state, out bool wasCreated);
            if (wasCreated)
                throw new ArgumentException("BUG: after exploring this state should have been cached");

            var cachedState = _cachedStates[nodeIdx];

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

        private void ExploreGameTree(byte[] startingState, bool isSimulation)
        {
            int playerTurn = 1;
            Array.Copy(startingState, _currentState, _currentState.Length);

            _selectedActions.Clear();
            while (true)
            {
                int idxStateNode = GetOrCreateCachedState(_currentState, out bool wasCreated);
                var cachedState = _cachedStates[idxStateNode];

                bool revisitedGameOver = cachedState.GameOver != GameOver.Status.GameIsNotOver;
                if (revisitedGameOver)
                {
                    cachedState.VisitCount++;

                    // This may repeat many times at the end of a simulation loop since the remaining simulations will just exploit this known winning state over and over.
                    _param.TextInfoCallback(LogLevel.Debug, $"revisiting a cached game over: {cachedState.GameOver}, nodeIdx: {cachedState.Idx}, player: {playerTurn}, moves: {_selectedActions.Count}");
                    BacktrackAndUpdate(_selectedActions, 1, currentPlayer: playerTurn);
                    break;
                }

                if (wasCreated)
                {
                    float expandV = ExpandState(ref cachedState, playerTurn);

                    // latest recorded action was the opponents, but v is for me, so negate v
                    BacktrackAndUpdate(_selectedActions, -expandV, currentPlayer: playerTurn);
                    break;
                }

                int selectedAction = RevisitNode(ref cachedState, isSimulation, playerTurn);

                _game.ExecutePlayerAction(_currentState, selectedAction);

                if (IsGameOver(ref cachedState, isSimulation, playerTurn, out float gameOverV))
                {
                    BacktrackAndUpdate(_selectedActions, 0, currentPlayer: playerTurn);
                    break;
                }

                _game.FlipStateToNextPlayer(_currentState);

                playerTurn = -playerTurn;
                //_param.TextInfoCallback(LogLevel.Debug, $"player switched from {-playerTurn} to {playerTurn}, nodeIdx: {cachedState.Idx}, player: {playerTurn}, moves: {_selectedActions.Count}");
            }
        }

        private bool IsGameOver(ref CachedState cachedState, bool isSimulation, int player, out float v)
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
                _param.TextInfoCallback(LogLevel.Debug, $"max moves reached, game over: {gameOverStatus}, nodeIdx: {cachedState.Idx}, player: {player}, moves: {_selectedActions.Count}");
                v = 0;
                return true;
            }

            cachedState.GameOver = gameOverStatus;

            // moves are always made as player1. the latest added actions
            // belongs to current player, no matter if this is pl1 or pl2.
            // so we want score from p1 perspective.
            v = GameOver.ValueForPlayer1(cachedState.GameOver);
            return true;
        }

        private int RevisitNode(ref CachedState cachedState, bool isSimulation, int player)
        {
            _param.TextInfoCallback(LogLevel.Debug, $"revisiting a cached state with visitCount {cachedState.VisitCount}, nodeIdx: {cachedState.Idx}, player: {player}, moves: {_selectedActions.Count}");

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

            for (int i = 0; i < cachedState.Actions.Length; i++)
            {
                ref Action action = ref cachedState.Actions[i];
                if (action.IsValidMove != 0)
                {
                    float actionProbability = addNoise ? (1 - _param.DirichletNoiseAmount) * action.ActionProbability + _param.DirichletNoiseAmount * _noiseReused[i] : action.ActionProbability;
                    float upperConfidence = action.Q + _param.Cpuct * actionProbability * (float)Math.Sqrt(cachedState.VisitCount) / (1.0f + action.VisitCount);

                    _param.TextInfoCallback(LogLevel.Debug, $"considering action: {i}, nodeIdx: {cachedState.Idx}, visitcount: {action.VisitCount}, q: {action.Q}, ucb: {upperConfidence}, prob: {action.ActionProbability}, player: {player}, moves: {_selectedActions.Count}, noise: {action.ActionProbability} -> {_noiseReused[i]} = {actionProbability}");
                    if (upperConfidence > bestUpperConfidence)
                    {
                        bestUpperConfidence = upperConfidence;
                        selectedAction = i;
                    }
                }
            }

            // an action was selected
            _selectedActions.Add(new SelectedAction { NodeIdx = cachedState.Idx, ActionIdx = selectedAction, Player = player });

            _param.TextInfoCallback(LogLevel.Debug, $"action selected: {selectedAction}, nodeIdx: {cachedState.Idx}, confidence: {bestUpperConfidence}, player: {player}, moves: {_selectedActions.Count}");

            cachedState.VisitCount++;

            return selectedAction;
        }

        private float ExpandState(ref CachedState cachedState, int playerTurn)
        {
            // get and save suggestions from Skynet, then backtrack to root using suggested v
            _skynet.Suggest(_currentState, playerTurn, _actionProbsReused, out float v);
            _game.GetValidActions(_currentState, _validActionsReused);

            ArrayUtil.FilterProbsByValidActions(_actionProbsReused, _validActionsReused);
            ArrayUtil.Normalize(_actionProbsReused);

            _param.TextInfoCallback(LogLevel.Debug, $"new state node created, nodeIdx: {cachedState.Idx}, network v: {v}, player: {playerTurn}, moves: {_selectedActions.Count}");

            bool hasValidActions = ArrayUtil.CountNonZero(_actionProbsReused) > 0;
            if (!hasValidActions)
            {
                // no valid actions in leaf, consider this a draw, ex TicTacToe: board is full
                _param.TextInfoCallback(LogLevel.Debug, $"no valid actions in new state node, nodeIdx: {cachedState.Idx}, player: {playerTurn}, moves: {_selectedActions.Count}");
                return 0;
            }

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
            _param.TextInfoCallback(LogLevel.Debug, $"backtracking sim result, v: {v}, moves/actions selected: {_selectedActions.Count}, initiated by player: {currentPlayer}");
            for (int i = selectedActions.Count - 1; i >= 0; --i)
            {
                currentPlayer = -currentPlayer;

                var node = _cachedStates[selectedActions[i].NodeIdx];
                int a = selectedActions[i].ActionIdx;
                ref var action = ref node.Actions[a];
                action.VisitCount++;

                float oldQ = action.Q;
                action.Q = ((action.VisitCount - 1) * action.Q + v) / action.VisitCount;
                _param.TextInfoCallback(LogLevel.Debug, $"updating nodeIdx {node.Idx}, actionIdx: {a}, v: {v}, oldQ: {oldQ}, newQ: {action.Q}, action visitCount: {action.VisitCount}, action taken by player: {selectedActions[i].Player}");

                // switch to the other players perspective
                v = -v;
            }
        }

        private int GetOrCreateCachedState(byte[] state, out bool wasCreated)
        {
            string key = Convert.ToBase64String(state);
            if (_cachedStateLookup.TryGetValue(key, out int idx))
            {
                wasCreated = false;
                return idx;
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

            return _stateIdx - 1;
        }
    }
}
