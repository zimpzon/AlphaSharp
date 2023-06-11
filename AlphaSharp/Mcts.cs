using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using AlphaSharp.Interfaces;
using Math = System.Math;

namespace AlphaSharp
{
    public class Mcts
    {
        public class SimStats
        {
            public int MaxMovesReached { get; set; }
            public int NoValidActions { get; set; }
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
            _state = new byte[_game.StateSize];
        }
        
        public void Reset()
        {
            _stateNodeLookup.Clear();
            _stateIdx = 0;
            Array.Clear(_stateNodes);
            Stats = new SimStats();
        }

        public float[] GetActionProbs(byte[] state, bool isSelfPlay)
        {
            var sw = Stopwatch.StartNew();

            int simCount = isSelfPlay ? _param.SelfPlaySimulationCount : _param.EvaluationSimulationCount;
            int explorationMaxMoves = isSelfPlay ? _param.SelfPlaySimulationMaxMoves: _param.EvaluationSimulationMaxMoves;

            for (int i = 0; i < simCount; i++)
            {
                ExploreGameTree(state, explorationMaxMoves, isSelfPlay);
                Stats.TotalSims++;
            }

            Stats.MsInSimulation = sw.Elapsed.TotalMilliseconds;

            int nodeIdx = GetOrCreateStateNodeFromState(state, out _);
            var stateNode = _stateNodes[nodeIdx];

            var probs = new float[stateNode.Actions.Length];
            if (!isSelfPlay)
            {
                int selectedAction = ActionUtil.PickActionByHighestVisitCount(stateNode.Actions);
                probs[selectedAction] = 1.0f;
                return probs;
            }

            int visitCountSum = stateNode.Actions.Sum(a => a.VisitCount);
            if (visitCountSum == 0)
                throw new InvalidOperationException($"no actions of the current state has any visit counts");

            // normalize visit counts to probs that sum to 1
            for (int i = 0; i < probs.Length; i++)
                probs[i] = stateNode.Actions[i].VisitCount / (float)visitCountSum;

            return probs;
        }

        private void ExploreGameTree(byte[] startingState, int maxMoves, bool isSelfPlay)
        {
            Array.Copy(startingState, _state, _state.Length);

            _selectedActions.Clear();

            int round = 0;
            while (true)
            {
                if (round++ >= maxMoves)
                {
                    // too many moves = draw
                    BacktrackAndUpdate(_selectedActions, 0);
                    Stats.MaxMovesReached++;
                    break;
                }

                int idxStateNode = GetOrCreateStateNodeFromState(_state, out bool isLeafNode);
                var stateNode = _stateNodes[idxStateNode];
                stateNode.VisitCount++;

                if (!isLeafNode)
                {
                    int numberOfValidActionsRevisited = ActionUtil.CountValidActions(stateNode.Actions);
                    if (numberOfValidActionsRevisited == 0)
                    {
                        // no valid actions in this revisited node, consider this a draw
                        BacktrackAndUpdate(_selectedActions, 0);
                        Stats.NoValidActions++;
                        break;
                    }
                }

                if (stateNode.GameOver == int.MinValue)
                    stateNode.GameOver = _game.GetGameEnded(_state);

                if (stateNode.GameOver != 0)
                {
                    // game result is always 1 for a won game. the winner was the opponent since player was already switched
                    BacktrackAndUpdate(_selectedActions, 1);
                    break;
                }

                if (isLeafNode)
                {
                    // get and save suggestions from Skynet, then backtrack to root using suggested v
                    var sw = Stopwatch.StartNew();
                    _skynet.Suggest(_state, _actionProbsTemp, out float v);
                    double ms = sw.Elapsed.TotalMilliseconds;
                    Stats.MsInSkynet += ms;
                    Stats.SkynetCalls++;

                    _game.GetValidActions(_state, _validActionsTemp);
                    int numberOfLeafValidActions = ArrayUtil.CountNonZero(_validActionsTemp);
                    if (numberOfLeafValidActions == 0)
                    {
                        // no valid actions in leaf, consider this a draw
                        BacktrackAndUpdate(_selectedActions, 0);
                        Stats.NoValidActions++;
                        break;
                    }

                    ArrayUtil.FilterProbsByValidActions(_actionProbsTemp, _validActionsTemp);
                    ArrayUtil.Normalize(_actionProbsTemp);

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

                if (isFirstMove && isSelfPlay)
                    Noise.CreateDirichlet(_noiseTemp, _param.DirichletNoiseShape);

                for (int i = 0; i < stateNode.Actions.Length; i++)
                {
                    ref StateNode.Action action = ref stateNode.Actions[i];
                    if (action.IsValidMove != 0)
                    {
                        float actionProbability = action.ActionProbability;
                        if (isFirstMove && isSelfPlay)
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

            _stateNodes[_stateIdx] = new StateNode(_game.ActionCount);
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

                action.Q = action.Q == 0.0f ? v : (action.VisitCount * action.Q + v) / (action.VisitCount + 1);

                // switch to the other players perspective
                v = -v;
            }
        }
    }
}
