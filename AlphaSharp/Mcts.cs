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
            public int NodesCreated { get; set; }
            public int NodesRevisited { get; set; }
            public int TotalSims { get; set; }
            public int TotalSimMoves { get; set; }
            public int SkynetCalls { get; set; }
            public float MsInSkynet { get; set; }
            public float MsInSimulation { get; set; }

            public override string ToString()
                => JsonSerializer.Serialize(this);
        }

        private class SelectedAction
        {
            public int NodeIdx;
            public int ActionIdx;
        }

        // states won't be reused much in a single simulation when moving forward, but when simulation ended
        // we backtrack and update all Q values. This means only very few actions are ever needed, so we waste
        // a lot of memory allocating 200. (200 * ~20 = 4000 per state) maybe 40mb for whole simulation. Meh, fine
        // my back and forth endless loop was not fixed by visitcounts since they were updated on the way back.

        public readonly SimStats Stats = new();

        private readonly IGame _game;
        private readonly ISkynet _skynet;
        private readonly Args _args;
        private StateNode[] _stateNodes = new StateNode[10000];
        private int _uniqueStateCount = 0;
        private readonly float[] _actionProbsTemp;
        private readonly float[] _noiseTemp;
        private readonly byte[] _validActionsTemp;
        private readonly List<SelectedAction> _selectedActions = new();
        private readonly byte[] _state;
        private readonly Dictionary<long, int> _stateNodeLookup = new();

        public Mcts(IGame game, ISkynet skynet, Args args)
        {
            _game = game;
            _skynet = skynet;
            _args = args;

            _actionProbsTemp = new float[_game.ActionCount];
            _noiseTemp = new float[_game.ActionCount];
            _validActionsTemp = new byte[_game.ActionCount];
            _state = new byte[_game.StateSize];
        }

        public float[] GetActionProbs(byte[] state, bool isTraining)
        {
            var sw = Stopwatch.StartNew();

            int simCount = isTraining ? _args.SimCountLearn : _args.SimCountPlay;

            for (int i = 0; i < simCount; i++)
            {
                ExploreGameTree(state, _args.SimMaxMoves, isTraining);
                Stats.TotalSims++;
            }

            Stats.MsInSimulation = sw.ElapsedMilliseconds;

            int nodeIdx = GetOrCreateStateNodeFromState(state, out _);
            var stateNode = _stateNodes[nodeIdx];

            var probs = new float[stateNode.Actions.Length];

            if (!isTraining)
            {
                int selectedAction = ActionUtil.PickActionByHighestVisitCount(stateNode.Actions);
                probs[selectedAction] = 1.0f;
                return probs;
            }

            //double temp = move_count < 20 ? 1 : 0.1;
            //counts = counts.Select(x => Math.Pow(x, 1.0 / temp)).ToArray();
            //double counts_sum = counts.Sum();

            int visitCountSum = stateNode.Actions.Sum(a => a.VisitCount);
            int validCount = stateNode.Actions.Count(a => a.IsValidMove != 0);

            if (visitCountSum == 0)
            {
                // No actions were visited for this state
                // This happens/can happen at the very last simulation step (at least in Python version)
                Console.WriteLine("WARNING: current main game state did not record any visitcounts, returning 1 for all actions");

                for (int i = 0; i < probs.Length; i++)
                    probs[i] = 1.0f / validCount;

                return probs;
            }

            // normalize visit counts to probs that sum to 1
            for (int i = 0; i < probs.Length; i++)
                probs[i] = stateNode.Actions[i].VisitCount / (float)visitCountSum;

            return probs;
        }

        private void ExploreGameTree(byte[] startingState, int maxMoves, bool isTraining)
        {
            Array.Copy(startingState, _state, _state.Length);

            _selectedActions.Clear();

            int player = 1; // we always start from the perspective of player 1
            const float DirichletAmount = 0.8f;

            while (true)
            {
                if (_selectedActions.Count >= maxMoves)
                {
                    // score is undetermined, use 0.0
                    BacktrackAndUpdate(_selectedActions, 0.0f);
                    Stats.MaxMovesReached++;
                    break;
                }

                int idxStateNode = GetOrCreateStateNodeFromState(_state, out bool isLeafNode);
                var stateNode = _stateNodes[idxStateNode];

                if (stateNode.GameOver == -1)
                    stateNode.GameOver = _game.GetGameEnded(_state);

                if (stateNode.GameOver != 0)
                {
                    BacktrackAndUpdate(_selectedActions, stateNode.GameOver * player);
                    break;
                }

                if (isLeafNode)
                {
                    // get and save suggestions from Skynet, then backtrack to root using suggested V.
                    var sw = Stopwatch.StartNew();
                    _skynet.Suggest(_state, _actionProbsTemp, out float v);
                    Stats.MsInSkynet += sw.ElapsedMilliseconds;
                    Stats.SkynetCalls++;

                    _game.GetValidActions(_state, _validActionsTemp);

                    ArrayUtil.FilterProbsByValidActions(_actionProbsTemp, _validActionsTemp);
                    ArrayUtil.Normalize(_actionProbsTemp);

                    for (int i = 0; i < stateNode.Actions.Length; ++i)
                    {
                        stateNode.Actions[i].ActionProbability = _actionProbsTemp[i];
                        stateNode.Actions[i].IsValidMove = _validActionsTemp[i];
                    }

                    BacktrackAndUpdate(_selectedActions, v);

                    player = 1;
                    Array.Copy(startingState, _state, _state.Length);
                    continue;
                }

                // revisited node, pick the action with the highest upper confidence bound
                stateNode.VisitCount++;

                float bestUpperConfidence = float.NegativeInfinity;
                int selectedAction = -1;

                bool isFirstMove = _selectedActions.Count == 0;

                if (isFirstMove && isTraining)
                    Noise.CreateDirichlet(_noiseTemp, DirichletAmount);

                for (int i = 0; i < stateNode.Actions.Length; i++)
                {
                    ref StateNode.Action action = ref stateNode.Actions[i];
                    if (action.IsValidMove != 0)
                    {
                        float actionProbability = action.ActionProbability;
                        if (isFirstMove && isTraining)
                            actionProbability = action.ActionProbability = (1 - DirichletAmount) * action.ActionProbability + DirichletAmount * _noiseTemp[i];

                        // if no Q value yet calc confidence without Q
                        float upperConfidence = action.Q == 0 ?
                            _args.Cpuct * actionProbability * (float)Math.Sqrt(stateNode.VisitCount + float.Epsilon) :
                            action.Q + _args.Cpuct * actionProbability * (float)Math.Sqrt(stateNode.VisitCount) / (1.0f + action.VisitCount);

                        if (upperConfidence > bestUpperConfidence)
                        {
                            bestUpperConfidence = upperConfidence;
                            selectedAction = i;
                        }
                    }
                }

                // an action was selected
                _selectedActions.Add(new SelectedAction { NodeIdx = idxStateNode, ActionIdx = selectedAction });
                stateNode.Actions[selectedAction].VisitCount++;

                _game.ExecutePlayerAction(_state, selectedAction);
                _game.FlipStateToNextPlayer(_state);
                Stats.TotalSimMoves++;

                player *= -1;
            }
        }

        private int GetOrCreateStateNodeFromState(byte[] state, out bool wasCreated)
        {
            long hash = Hash.ComputeHash(state);
            if (_stateNodeLookup.TryGetValue(hash, out int idx))
            {
                Stats.NodesRevisited++;
                wasCreated = false;
                return idx;
            }

            // Does not exist, create it and add to lookup
            if (_uniqueStateCount >= _stateNodes.Length)
            {
                Console.WriteLine($"expanding stateNode array from {_stateNodes.Length} to {_stateNodes.Length * 2}");
                Array.Resize(ref _stateNodes, _stateNodes.Length * 2);
            }

            _stateNodes[_uniqueStateCount] = new StateNode(_game.ActionCount);
            _stateNodeLookup.Add(hash, _uniqueStateCount);

            _uniqueStateCount++;
            Stats.NodesCreated++;
            wasCreated = true;

            return _uniqueStateCount - 1;
        }

        private void BacktrackAndUpdate(List<SelectedAction> selectedActions, float v)
        {
            // v is for current player, start by negating it since good for us = bad for them and vice versa
            for (int i = selectedActions.Count - 1; i >= 0; --i)
            {
                v = -v;

                var node = _stateNodes[selectedActions[i].NodeIdx];
                int a = selectedActions[i].ActionIdx;
                ref var action = ref node.Actions[a];

                action.Q = action.Q == 0.0f ? v : (action.VisitCount * action.Q + v) / (action.VisitCount + 1);
            }
        }
    }
}
