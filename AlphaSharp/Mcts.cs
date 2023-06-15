using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using AlphaSharp.Interfaces;
using Math = System.Math;

namespace AlphaSharp
{
    public class Mcts
    {
        public class SimStats
        {
            public int NodesCreated { get; set; }
            public int NodesRevisited { get; set; }
            public int TotalSims { get; set; }
            public int TotalSimMoves { get; set; }
            public int SkynetCalls { get; set; }
            public double MsInSkynet { get; set; }
            public double MsInSimulation { get; set; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        private sealed class SelectedAction
        {
            public int NodeIdx;
            public int ActionIdx;
            public float Player;
        }

        public SimStats Stats { get; private set; } = new();

        private readonly IGame _game;
        private readonly ISkynet _skynet;
        private readonly AlphaParameters _param;
        private StateNode[] _stateNodes = new StateNode[200000];
        private int _stateIdx = 0;
        private readonly float[] _actionProbsTemp;
        private readonly float[] _noiseTemp;
        private readonly double[] _actionsVisitCountTemp;
        private readonly byte[] _validActionsTemp;
        private readonly List<SelectedAction> _selectedActions = new();
        private readonly byte[] _state;

        // could use Zobrist hashing instead of base64 key. https://en.wikipedia.org/wiki/Zobrist_hashing
        // however, this requires the game to expose the number of possible values per cell.
        // right now bottleneck is nn inference anyways.
        private readonly Dictionary<string, int> _stateNodeLookup = new();

        public Mcts(IGame game, ISkynet skynet, AlphaParameters args)
        {
            _game = game;
            _skynet = skynet;
            _param = args;

            _actionProbsTemp = new float[_game.ActionCount];
            _noiseTemp = new float[_game.ActionCount];
            _validActionsTemp = new byte[_game.ActionCount];
            _actionsVisitCountTemp = new double[_game.ActionCount];
            _state = new byte[_game.StateSize];
        }
        
        public void Reset()
        {
            _stateNodeLookup.Clear();
            _stateIdx = 0;
            Array.Clear(_stateNodes);
            Stats = new SimStats();
        }

        public float[] GetActionProbs(byte[] state)
        {
            var sw = Stopwatch.StartNew();

            int simCount = _param.SimulationIterations;

            for (int i = 0; i < simCount; i++)
            {
                ExploreGameTree(state, isSimulation: false);
                Stats.TotalSims++;
            }

            Stats.MsInSimulation = sw.Elapsed.TotalMilliseconds;

            int nodeIdx = GetOrCreateStateNodeFromState(state, out _);
            var stateNode = _stateNodes[nodeIdx];

            var probs = new float[stateNode.Actions.Length];
            int selectedAction = ActionUtil.PickActionByHighestVisitCount(stateNode.Actions);
            probs[selectedAction] = 1.0f;
            return probs;
        }

        public float[] GetActionProbsForSelfPlay(byte[] state, float temperature)
        {
            var sw = Stopwatch.StartNew();

            int simCount = _param.SimulationIterations;

            for (int i = 0; i < simCount; i++)
            {
                ExploreGameTree(state, isSimulation: true);
                Stats.TotalSims++;
            }

            Stats.MsInSimulation = sw.Elapsed.TotalMilliseconds;

            int nodeIdx = GetOrCreateStateNodeFromState(state, out _);
            var stateNode = _stateNodes[nodeIdx];

            var probs = new float[stateNode.Actions.Length];

            for (int i = 0; i < _actionsVisitCountTemp.Length; i++)
                probs[i] = stateNode.Actions[i].VisitCount;

            ArrayUtil.Softmax(probs, temperature);
            return probs;
        }

        private int GetOrCreateStateNodeFromState(byte[] state, out bool wasCreated)
        {
            string key = Convert.ToBase64String(state);
            if (_stateNodeLookup.TryGetValue(key, out int idx))
            {
                Stats.NodesRevisited++;
                wasCreated = false;
                return idx;
            }

            if (_stateIdx >= _stateNodes.Length)
            {
                int newSize = (int)(_stateNodes.Length * 1.5);
                _param.TextInfoCallback(LogLevel.MoreInfo, $"expanding stateNode array from {_stateNodes.Length} to {newSize}");

                Array.Resize(ref _stateNodes, newSize);
            }

            _stateNodes[_stateIdx] = new StateNode(_game.ActionCount, _stateIdx);
            _stateNodeLookup.Add(key, _stateIdx);

            _stateIdx++;
            Stats.NodesCreated++;
            wasCreated = true;

            return _stateIdx - 1;
        }

        private void ExploreGameTree(byte[] startingState, bool isSimulation)
        {
            float player = 1;
            Array.Copy(startingState, _state, _state.Length);

            _selectedActions.Clear();
            while (true)
            {
                int idxStateNode = GetOrCreateStateNodeFromState(_state, out bool wasCreated);
                var stateNode = _stateNodes[idxStateNode];

                if (stateNode.GameOver != GameOver.Status.GameIsNotOver)
                {
                    // Revisiting a game over state. We can get here when simulation is over and
                    // the "real" game makes a winning move that was visited during the simulation.
                    _param.TextInfoCallback(LogLevel.Debug, $"revisiting a cached game state: {stateNode.GameOver}, nodeIdx: {stateNode.Idx}, player: {player}, moves: {_selectedActions.Count}");
                    BacktrackAndUpdate(_selectedActions, 1, currentPlayer: player);
                    break;
                }

                if (wasCreated)
                {
                    // get and save suggestions from Skynet, then backtrack to root using suggested v
                    var sw = Stopwatch.StartNew();
                    _skynet.Suggest(_state, _actionProbsTemp, out float v);
                    double ms = sw.Elapsed.TotalMilliseconds;
                    Stats.MsInSkynet += ms;
                    Stats.SkynetCalls++;
                    _game.GetValidActions(_state, _validActionsTemp);

                    ArrayUtil.FilterProbsByValidActions(_actionProbsTemp, _validActionsTemp);
                    ArrayUtil.Normalize(_actionProbsTemp);

                    _param.TextInfoCallback(LogLevel.Debug, $"new state node created, nodeIdx: {stateNode.Idx}, network v: {v}, player: {player}, moves: {_selectedActions.Count}");

                    bool hasValidActions = ArrayUtil.CountNonZero(_actionProbsTemp) > 0;
                    if (!hasValidActions)
                    {
                        // no valid actions in leaf, consider this a draw, ex TicTacToe: board is full
                        _param.TextInfoCallback(LogLevel.Debug, $"no valid actions in new state node, nodeIdx: {stateNode.Idx}, player: {player}, moves: {_selectedActions.Count}");
                        BacktrackAndUpdate(_selectedActions, 0, currentPlayer: player);
                        break;
                    }

                    for (int i = 0; i < stateNode.Actions.Length; ++i)
                    {
                        stateNode.Actions[i].ActionProbability = _actionProbsTemp[i];
                        stateNode.Actions[i].IsValidMove = _validActionsTemp[i];
                    }

                    stateNode.VisitCount = 1;

                    // latest recorded action was the opponents, but v is for me, so negate v
                    BacktrackAndUpdate(_selectedActions, -v, currentPlayer: player);
                    break;
                }

                // revisited node, pick the action with the highest upper confidence bound

                float bestUpperConfidence = float.NegativeInfinity;
                int selectedAction = -1;

                bool isFirstMove = _selectedActions.Count == 0;

                if (isFirstMove && isSimulation && !stateNode.HasNoise)
                {
                    Noise.CreateDirichlet(_noiseTemp, _param.DirichletNoiseShape);
                    for (int i = 0; i < stateNode.Actions.Length; i++)
                    {
                        ref StateNode.Action action = ref stateNode.Actions[i];
                        if (action.IsValidMove != 0)
                            action.ActionProbability = (1 - _param.DirichletNoiseAmount) * action.ActionProbability + _param.DirichletNoiseAmount * _noiseTemp[i];
                    }

                    stateNode.HasNoise = true;
                }

                for (int i = 0; i < stateNode.Actions.Length; i++)
                {
                    ref StateNode.Action action = ref stateNode.Actions[i];
                    if (action.IsValidMove != 0)
                    {
                        _param.TextInfoCallback(LogLevel.Debug, $"considering action: {i}, nodeIdx: {stateNode.Idx}, visitcount: {action.VisitCount}, q: {action.Q}, prob: {action.ActionProbability}, player: {player}, moves: {_selectedActions.Count}");

                        float actionProbability = action.ActionProbability;
                        float upperConfidence = action.Q + _param.Cpuct * actionProbability * (float)Math.Sqrt(stateNode.VisitCount) / (1.0f + action.VisitCount);
                        if (upperConfidence > bestUpperConfidence)
                        {
                            bestUpperConfidence = upperConfidence;
                            selectedAction = i;
                        }
                    }
                }

                // an action was selected
                _selectedActions.Add(new SelectedAction { NodeIdx = idxStateNode, ActionIdx = selectedAction, Player = player });

                _param.TextInfoCallback(LogLevel.Debug, $"executing selected action: {selectedAction}, nodeIdx: {stateNode.Idx}, confidence: {bestUpperConfidence}, player: {player}, moves: {_selectedActions.Count}");

                _game.ExecutePlayerAction(_state, selectedAction);
                var gameState = _game.GetGameEnded(_state, _selectedActions.Count, isSimulation);

                if (gameState != GameOver.Status.GameIsNotOver)
                {
                    if (gameState == GameOver.Status.DrawDueToMaxMovesReached)
                    {
                        // do not mark state as a draw, we could get here later without having reached max moves
                        _param.TextInfoCallback(LogLevel.Debug, $"max moves reached, game over: {stateNode.GameOver}, nodeIdx: {stateNode.Idx}, player: {player}, moves: {_selectedActions.Count}");
                        BacktrackAndUpdate(_selectedActions, 0, currentPlayer: player);
                        break;
                    }

                    // mark state with the game over result
                    stateNode.GameOver = gameState;

                    // moves are always made as player1. the latest added actions
                    // belongs to current player, no matter if this is pl1 or pl2.
                    // so we want score from p1 perspective.
                    float v = GameOver.ValueForPlayer1(stateNode.GameOver);
                    _param.TextInfoCallback(LogLevel.Debug, $"game over after executing action, game over: {stateNode.GameOver}, nodeIdx: {stateNode.Idx}, player: {player}, moves: {_selectedActions.Count}, game v: {v}");
                    BacktrackAndUpdate(_selectedActions, v, currentPlayer: player);
                    break;
                }

                _game.FlipStateToNextPlayer(_state);
                player = -player;
                _param.TextInfoCallback(LogLevel.Debug, $"player switched from {-player} to {player}, nodeIdx: {stateNode.Idx}, player: {player}, moves: {_selectedActions.Count}");

                Stats.TotalSimMoves++;
            }
        }

        private void BacktrackAndUpdate(List<SelectedAction> selectedActions, float v, float currentPlayer)
        {
            _param.TextInfoCallback(LogLevel.Debug, $"backtracking sim result, v: {v}, moves/actions selected: {_selectedActions.Count}, initiated by player: {currentPlayer}");
            for (int i = selectedActions.Count - 1; i >= 0; --i)
            {
                currentPlayer = -currentPlayer;

                var node = _stateNodes[selectedActions[i].NodeIdx];
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
    }
}
