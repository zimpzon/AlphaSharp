using System;
using System.Collections.Generic;
using System.Diagnostics;
using AlphaSharp.Interfaces;
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

        // states won't be reused much in a single simulation when moving forward, but when simulation ended
        // we backtrack and update all Q values. This means only very few actions are ever needed, so we waste
        // a lot of memory allocating 200. (200 * ~20 = 4000 per state) maybe 40mb for whole simulation. Meh, fine
        // my back and forth endless loop was not fixed by visitcounts since they were updated on the way back.

        private readonly IGame _game;
        private readonly ISkynet _skynet;
        private readonly Args _args;
        private StateNode[] _stateNodes = new StateNode[10000];
        private readonly object _createStateNodeLock = new();
        private readonly SimStats _simStats = new();
        private int _stateNodeCount = 0;

        private readonly Dictionary<byte[], int> _stateNodeLookup = new(new ByteArrayComparer());

        public Mcts(IGame game, ISkynet skynet, Args args)
        {
            _game = game;
            _skynet = skynet;
            _args = args;
        }

        private StateNode GetStateNodeFromIndex(int idx)
        {
            lock (_createStateNodeLock)
            {
                return _stateNodes[idx];
            }
        }

        private int GetOrCreateStateNodeFromState(byte[] state, out bool wasCreated)
        {
            lock (_createStateNodeLock)
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
        }

        public void ExploreGameTree(byte[] startingState, int moveCount, int maxMoves, bool isTraining)
        {
            var state = new byte[startingState.Length];
            Array.Copy(startingState, state, state.Length);

            var actionProbsTemp = new float[_game.ActionCount];
            var noiseTemp = new float[_game.ActionCount];
            var validActionsTemp = new byte[_game.ActionCount];
            var selectedActions = new int[maxMoves];

            int player = 1; // we always start from the perspective of player 1
            const float DirichletAmount = 0.8f;
            const int RootIdx = 0;
            int currentIndex = -1;

            while (true)
            {
                // if maxDepth reached don't draw any conclusions, just return 0
                if (moveCount++ >= maxMoves)
                {
                    BacktrackAndUpdate(GetStateNodeFromIndex(currentIndex), selectedActions, moveCount, 0.0f);
                    _simStats.MaxMovesReached++;
                    break;
                }

                // get or create node for current state
                int idxNewStateNode = GetOrCreateStateNodeFromState(state, out bool wasCreated);

                var stateNode = GetStateNodeFromIndex(idxNewStateNode);
                SimpleLock.AcquireLock(ref stateNode.Lock);

                stateNode.ParentIndex = currentIndex;
                currentIndex = idxNewStateNode;

                // if game over not determined for state, do it now
                if (stateNode.GameOver == -1)
                    stateNode.GameOver = _game.GetGameEnded(state);

                if (stateNode.GameOver != 0)
                {
                    // game determined, stop sim and update tree back to root
                    BacktrackAndUpdate(stateNode, selectedActions, moveCount, stateNode.GameOver * player);

                    SimpleLock.ReleaseLock(ref stateNode.Lock);
                    break;
                }
                else
                {
                    // not game over
                    if (wasCreated)
                    {
                        // leaf node, get and save suggestions from Skynet, then backtrack to root using suggested V.
                        var sw = Stopwatch.StartNew();
                        {
                            _skynet.Suggest(state, actionProbsTemp, out stateNode.V);
                            _simStats.MsInSkynet += sw.ElapsedMilliseconds;
                        }

                        _game.GetValidActions(state, validActionsTemp);

                        // save action probs and valid moves for state
                        for (int i = 0; i < stateNode.Actions.Length; ++i)
                        {
                            stateNode.Actions[i].ActionProbability = actionProbsTemp[i];
                            stateNode.Actions[i].IsValidMove = validActionsTemp[i];
                        }

                        bool isFirstMove = moveCount == 0;
                        if (isFirstMove && isTraining)
                            Noise.AddDirichlet(actionProbsTemp, noiseTemp, DirichletAmount);

                        // exclude invalid action suggestions
                        ArrayUtil.FilterProbsByValidActions(actionProbsTemp, validActionsTemp);

                        // normalize so sum of probs is 1
                        ArrayUtil.Normalize(actionProbsTemp);

                        BacktrackAndUpdate(stateNode, selectedActions, moveCount, stateNode.V);

                        currentIndex = RootIdx;
                        SimpleLock.ReleaseLock(ref stateNode.Lock);
                        continue;
                    }

                    // revisited node, pick the action with the highest upper confidence bound
                    stateNode.VisitCount++;

                    float bestUpperConfidence = float.NegativeInfinity;
                    int bestAction = -1;

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
                                bestAction = i;
                            }
                        }
                    }

                    // an action was selected
                    selectedActions[moveCount] = bestAction;
                    stateNode.Actions[bestAction].VisitCount++;

                    _game.ExecutePlayerAction(state, bestAction);
                    _game.FlipStateToNextPlayer(state);

                    player *= -1;
                    SimpleLock.ReleaseLock(ref stateNode.Lock);
                }
            }
        }

        private void BacktrackAndUpdate(StateNode fromNode, int[] selectedActions, int moveIdx, float v)
        {
            // gameResult is for current player (fromNode), start with switch to opposing player.
            var currentNode = fromNode;
            while (currentNode.ParentIndex >= 0)
            {
                v = -v;
                currentNode = GetStateNodeFromIndex(currentNode.ParentIndex);

                int a = selectedActions[moveIdx];
                ref var action = ref currentNode.Actions[a];

                action.Q = action.Q == 0.0f ? v : (action.VisitCount * action.Q + v) / (action.VisitCount + 1);
            }
        }

        public double[] GetActionProbs(byte[] state, bool isTraining, int numberOfSim, int simMaxMoves)
        {
            // NB NB NB: TorchSharp is threadsafe but not tasksafe! Do manual threading.
            for (int i = 0; i < numberOfSim; i++)
                ExploreGameTree(state, moveCount: 0, simMaxMoves, isTraining);

            int nodeIdx = GetOrCreateStateNodeFromState(state, out _);
            var stateNode = GetStateNodeFromIndex(nodeIdx);

            if (!isTraining)
            {
                //ActionUtil.PickBestActionFromProbs(stateNode.Actions)
                //int[] bestAs = Enumerable.Range(0, counts.Length).Where(a => counts[a] == counts.Max()).ToArray();
                //int bestA = bestAs[new Random().Next(bestAs.Length)];
                //double[] probs = Enumerable.Repeat(1.0 / counts.Length, counts.Length).ToArray();
                //probs[bestA] = 1;
                return null;
            }

            //double temp = move_count < 20 ? 1 : 0.1;

            //counts = counts.Select(x => Math.Pow(x, 1.0 / temp)).ToArray();
            //double counts_sum = counts.Sum();

            //if (counts_sum == 0)
            //{
            //    // No actions were visited for this state
            //    // This happens/can happen at the very last simulation step
            //    Console.WriteLine("WARNING: current main game state did not record any visitcounts, returning 1 for all actions");
            //    bool[] valids = game.GetValidMoves(board, 1);
            //    double[] probs = Enumerable.Repeat(1.0 / counts.Length, counts.Length).ToArray();
            //    for (int i = 0; i < probs.Length; i++)
            //    {
            //        probs[i] *= valids[i] ? 1 : 0;
            //    }
            //    double sum_probs = probs.Sum();
            //    probs = probs.Select(x => x / sum_probs).ToArray();
            //    return probs;
            //}

            //double[] probs_normalized = counts.Select(x => x / counts_sum).ToArray();
            return null;
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
