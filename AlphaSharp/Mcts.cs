﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private class SelectedAction
        {
            public int NodeIdx;
            public int ActionIdx;
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

            // adjust by temperature
            for (int i = 0; i < _actionsVisitCountTemp.Length; i++)
                probs[i] = (float)Math.Pow(stateNode.Actions[i].VisitCount, 1.0f / temperature);

            for (int i = 0; i < probs.Length; i++)
            {
                // When visitcounts get really high (millions) the Math.Pow can exceed float max and is then Infinity. Fix this :-)
                if (float.IsNaN(probs[i]) || float.IsInfinity(probs[i]) || float.IsNegativeInfinity(probs[i]))
                    probs[i] = float.MaxValue;
            }

            ArrayUtil.Normalize(probs);
            return probs;
        }

        private void ExploreGameTree(byte[] startingState, bool isSimulation)
        {
            Array.Copy(startingState, _state, _state.Length);

            _selectedActions.Clear();

            while (true)
            {
                int idxStateNode = GetOrCreateStateNodeFromState(_state, out bool wasCreated);
                var stateNode = _stateNodes[idxStateNode];
                stateNode.VisitCount++;

                if (stateNode.GameOver != GameOver.Status.GameIsNotOver)
                {
                    // Revisiting a game over state. We can get here when simulation is over and
                    // the "real" game makes a winning move that was visited during the simulation.
                    BacktrackAndUpdate(_selectedActions, 1);
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

                    bool hasValidActions = ArrayUtil.CountNonZero(_actionProbsTemp) > 0;
                    if (!hasValidActions)
                    {
                        // no valid actions in leaf, consider this a draw, ex TicTacToe: board is full
                        BacktrackAndUpdate(_selectedActions, 0);
                        break;
                    }

                    for (int i = 0; i < stateNode.Actions.Length; ++i)
                    {
                        stateNode.Actions[i].ActionProbability = _actionProbsTemp[i];
                        stateNode.Actions[i].IsValidMove = _validActionsTemp[i];
                    }

                    // latest recorded action was the opponents, but v is for me, so negate v
                    BacktrackAndUpdate(_selectedActions, -v);
                    break;
                }

                // revisited node, pick the action with the highest upper confidence bound

                float bestUpperConfidence = float.NegativeInfinity;
                int selectedAction = -1;

                bool isFirstMove = _selectedActions.Count == 0;

                if (isFirstMove && isSimulation)
                    Noise.CreateDirichlet(_noiseTemp, _param.DirichletNoiseShape);

                for (int i = 0; i < stateNode.Actions.Length; i++)
                {
                    ref StateNode.Action action = ref stateNode.Actions[i];
                    if (action.IsValidMove != 0)
                    {
                        float actionProbability = action.ActionProbability;
                        if (isFirstMove && isSimulation)
                            actionProbability = (1 - _param.DirichletNoiseAmount) * action.ActionProbability + _param.DirichletNoiseAmount * _noiseTemp[i];

                        // if no Q value yet calc confidence without Q
                        float upperConfidence = action.Q == 0 ?
                            _param.Cpuct * actionProbability * (float)Math.Sqrt(stateNode.VisitCount + float.Epsilon) :
                            action.Q + _param.Cpuct * actionProbability * (float)Math.Sqrt(stateNode.VisitCount) / (1.0f + action.VisitCount);

                        if (upperConfidence > bestUpperConfidence)
                        {
                            bestUpperConfidence = upperConfidence;
                            selectedAction = i;
                        }
                    }
                }

                // an action was selected
                _selectedActions.Add(new SelectedAction { NodeIdx = idxStateNode, ActionIdx = selectedAction });

                _game.ExecutePlayerAction(_state, selectedAction);
                stateNode.GameOver = _game.GetGameEnded(_state, _selectedActions.Count, isSimulation);

                if (stateNode.GameOver != GameOver.Status.GameIsNotOver)
                {
                    // no matter who won the value for current player is 1 (or 0 for draw).
                    float v = Math.Abs(GameOver.ValueForPlayer1(stateNode.GameOver));
                    BacktrackAndUpdate(_selectedActions, v);
                    break;
                }

                _game.FlipStateToNextPlayer(_state);

                Stats.TotalSimMoves++;
            }
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

        private void BacktrackAndUpdate(List<SelectedAction> selectedActions, float v)
        {
            for (int i = selectedActions.Count - 1; i >= 0; --i)
            {
                var node = _stateNodes[selectedActions[i].NodeIdx];
                int a = selectedActions[i].ActionIdx;
                ref var action = ref node.Actions[a];
                action.VisitCount++;

                action.Q = action.Q == 0.0f ? (v + float.Epsilon) : (action.VisitCount * action.Q + v) / (action.VisitCount + 1);

                // switch to the other players perspective
                v = -v;
            }
        }
    }
}
