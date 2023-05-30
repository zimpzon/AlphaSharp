using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AlphaSharp.Interfaces;
using static AlphaSharp.StateNode;
using Math = System.Math;

namespace AlphaSharp
{
    public class Mcts
    {
        class SimStats
        {
            public int MaxMovesReached;
            public int NodesCreated;
            public int NodesRevisited;
            public float MsInSkynet;
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

        private readonly IGame _game;
        private readonly ISkynet _skynet;
        private readonly Args _args;
        private StateNode[] _stateNodes = new StateNode[10000];
        private readonly SimStats _simStats = new();
        private int _stateNodeCount = 0;

        private readonly Dictionary<byte[], int> _stateNodeLookup = new(new ByteArrayComparer());

        public Mcts(IGame game, ISkynet skynet, Args args)
        {
            _game = game;
            _skynet = skynet;
            _args = args;
        }

        private int GetOrCreateStateNodeFromState(byte[] state, out bool wasCreated)
        {
            if (_stateNodeLookup.TryGetValue(state, out int idx))
            {
                _simStats.NodesRevisited++;
                wasCreated = false;
                return idx;
            }

            // Does not exist, create it and add to lookup
            if (_stateNodeCount >= _stateNodes.Length)
            {
                Console.WriteLine($"expanding stateNode array from {_stateNodes.Length} to {_stateNodes.Length * 2}");
                Array.Resize(ref _stateNodes, _stateNodes.Length * 2);
            }

            _stateNodes[_stateNodeCount] = new StateNode(_game.ActionCount);
            _stateNodeLookup.Add(state, _stateNodeCount);

            _stateNodeCount++;
            _simStats.NodesCreated++;
            wasCreated = true;

            return _stateNodeCount - 1;
        }

        public void ExploreGameTree(byte[] startingState, int maxMoves, bool isTraining)
        {
            var state = new byte[startingState.Length];
            Array.Copy(startingState, state, state.Length);

            var actionProbsTemp = new float[_game.ActionCount];
            var noiseTemp = new float[_game.ActionCount];
            var validActionsTemp = new byte[_game.ActionCount];
            List<SelectedAction> selectedActions = new ();

            int player = 1; // we always start from the perspective of player 1
            const float DirichletAmount = 0.8f;

            while (true)
            {
                if (selectedActions.Count >= maxMoves)
                {
                    // score is undetermined, use 0.0
                    BacktrackAndUpdate(selectedActions, 0.0f);
                    _simStats.MaxMovesReached++;
                    break;
                }

                // get or create node for current state
                int idxStateNode = GetOrCreateStateNodeFromState(state, out bool wasCreated);
                var stateNode = _stateNodes[idxStateNode];

                // if game over not determined for state, do it now
                if (stateNode.GameOver == -1)
                    stateNode.GameOver = _game.GetGameEnded(state);

                if (stateNode.GameOver != 0)
                {
                    // game determined, stop sim and update tree back to root
                    BacktrackAndUpdate(selectedActions, stateNode.GameOver * player);
                    break;
                }
                else
                {
                    // not game over
                    if (wasCreated)
                    {
                        // leaf node, get and save suggestions from Skynet, then backtrack to root using suggested V.
                        var sw = Stopwatch.StartNew();
                        _skynet.Suggest(state, actionProbsTemp, out float v);
                        _simStats.MsInSkynet += sw.ElapsedMilliseconds;

                        _game.GetValidActions(state, validActionsTemp);

                        // save action probs and valid moves for state
                        for (int i = 0; i < stateNode.Actions.Length; ++i)
                        {
                            stateNode.Actions[i].ActionProbability = actionProbsTemp[i];
                            stateNode.Actions[i].IsValidMove = validActionsTemp[i];
                        }

                        bool isFirstMove = selectedActions.Count == 0;
                        if (isFirstMove && isTraining)
                            Noise.AddDirichlet(actionProbsTemp, noiseTemp, DirichletAmount);

                        // exclude invalid action suggestions
                        ArrayUtil.FilterProbsByValidActions(actionProbsTemp, validActionsTemp);

                        // normalize so sum of probs is 1
                        ArrayUtil.Normalize(actionProbsTemp);

                        BacktrackAndUpdate(selectedActions, v);

                        continue;
                    }

                    // revisited node, pick the action with the highest upper confidence bound
                    stateNode.VisitCount++;

                    float bestUpperConfidence = float.NegativeInfinity;
                    int selectedAction = -1;

                    for (int i = 0; i < stateNode.Actions.Length; i++)
                    {
                        ref StateNode.Action action = ref stateNode.Actions[i];
                        if (action.IsValidMove != 0)
                        {
                            // if no Q value yet calc confidence without Q
                            float upperConfidence = action.Q == 0 ?
                                _args.cpuct * action.ActionProbability * (float)Math.Sqrt(stateNode.VisitCount + float.Epsilon) :
                                action.Q + _args.cpuct * action.ActionProbability * (float)Math.Sqrt(stateNode.VisitCount) / (1.0f + action.VisitCount);

                            if (upperConfidence > bestUpperConfidence)
                            {
                                bestUpperConfidence = upperConfidence;
                                selectedAction = i;
                            }
                        }
                    }

                    // an action was selected
                    selectedActions.Add(new SelectedAction { NodeIdx = idxStateNode, ActionIdx = selectedAction });
                    stateNode.Actions[selectedAction].VisitCount++;

                    _game.ExecutePlayerAction(state, selectedAction);
                    _game.FlipStateToNextPlayer(state);

                    player *= -1;

                }
            }
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

        public float[] GetActionProbs(byte[] state, bool isTraining, int numberOfSim, int simMaxMoves)
        {
            for (int i = 0; i < numberOfSim; i++)
                ExploreGameTree(state, simMaxMoves, isTraining);

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
    }

    public class Args
    {
        public int numMCTSSims;
        public int numMCTSPlay;
        public int maxMCTSDepth;
        public float cpuct;
    }
}
