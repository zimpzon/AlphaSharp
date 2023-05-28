using AlphaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Math = System.Math;

namespace MCTSExample
{
    public class MCTS
    {
        struct Action
        {
            public float Q;
            public int VisitCount;
            public int ChildIndex;
            public float IsValidMove;
            public float ActionValue;
        }

        struct StateNode
        {
            public StateNode(int actionCount)
            {
                GameOver = -1;
                ParentIndex = -1;
                Actions = new Action[actionCount];
            }

            public long Lock;
            public int VisitCount;
            public int GameOver;
            public int ParentIndex;
            public Action[] Actions;
        }

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

        private IGame _game;
        private ISkynet _skynet;
        private Args _args;
        private bool isTraining;
        private StateNode[] _stateNodes = new StateNode[1000];
        private object _createStateNodeLock = new();
        private SimStats _simStats = new ();
        private int _stateNodeCount = 0;

        private Dictionary<byte[], int> _stateNodeLookup = new(new ByteArrayComparer());

        public MCTS(IGame game, ISkynet skynet, Args args)
        {
            _game = game;
            _skynet = skynet;
            _args = args;
        }

        private int GetOrCreateStateNode(byte[] state, out bool wasCreated)
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
                _stateNodes[_stateNodeCount] = new StateNode(_game.ActionCount);
                _stateNodeLookup.Add(state, _stateNodeCount);

                _stateNodeCount++;
                _simStats.NodesCreated++;
                wasCreated = true;

                return _stateNodeCount - 1;
            }
        }

        private void ExploreGameTree(byte[] startingState, int moveCount, int maxMoves)
        {
            int parentIndex = -1;

            var state = new float[startingState.Length];
            Array.Copy(startingState, state, state.Length);

            var actionProbs = new float[_game.ActionCount];
            var noiseTemp = new float[_game.ActionCount];

            while (true)
            {
                // if maxDepth return draw
                if (moveCount++ >= maxMoves)
                {
                    _simStats.MaxMovesReached++;
                    return;
                }

                // get or create node for current state
                int idxCurrentStateNode = GetOrCreateStateNode(startingState, out bool wasCreated);

                ref StateNode stateNode = ref _stateNodes[idxCurrentStateNode];
                SimpleLock.AcquireLock(ref stateNode.Lock);

                // if game over not determined for state, do it
                if (stateNode.GameOver == -1)
                    stateNode.GameOver = _game.GetGameEnded(startingState);

                if (stateNode.GameOver != 0)
                {
                    // stop sim and update tree back to root
                    stateNode.VisitCount = 1;
                    BacktrackAndUpdate(ref stateNode, stateNode.GameOver);
                }
                else
                {
                    // not game over
                    if (wasCreated)
                    {
                        var sw = Stopwatch.StartNew();
                        _skynet.Suggest(state, actionProbs, out float v);
                        _simStats.MsInSkynet += sw.ElapsedMilliseconds;
                        Noise.AddDirichlet(actionProbs, noiseTemp, 0.8f);
                    }
                    else
                    {
                        // 
                    }
                }
                SimpleLock.ReleaseLock(ref stateNode.Lock);

                parentIndex = idxCurrentStateNode;
            }


            // if gameEnded this return gameEnded
        }

        private void BacktrackAndUpdate(ref StateNode fromNode, int gameResult)
        {
            ref StateNode currentNode = ref fromNode;
            while (currentNode.ParentIndex > 0)
            {
                ref StateNode stateNode = ref _stateNodes[fromNode.ParentIndex];
            }
        }

        public double[] GetActionProbs(byte[] state, bool is_training, int numberOfSim, int simMaxMoves)
        {
            for (int i = 0; i < numberOfSim; i++)
                ExploreGameTree(state, 0, simMaxMoves);

            // Visit counts for all actions in current state s = how many times the action was taken
            double[] counts = new double[game.GetActionSize()];
            for (int a = 0; a < counts.Length; a++)
            {
                if (visitcount_stateaction.ContainsKey((s, a)))
                    counts[a] = visitcount_stateaction[(s, a)];
            }

            if (!is_training)
            {
                int[] bestAs = Enumerable.Range(0, counts.Length).Where(a => counts[a] == counts.Max()).ToArray();
                int bestA = bestAs[new Random().Next(bestAs.Length)];
                double[] probs = Enumerable.Repeat(1.0 / counts.Length, counts.Length).ToArray();
                probs[bestA] = 1;
                return probs;
            }

            double temp = move_count < 20 ? 1 : 0.1;

            counts = counts.Select(x => Math.Pow(x, 1.0 / temp)).ToArray();
            double counts_sum = counts.Sum();

            if (counts_sum == 0)
            {
                // No actions were visited for this state
                // This happens/can happen at the very last simulation step
                Console.WriteLine("WARNING: current main game state did not record any visitcounts, returning 1 for all actions");
                bool[] valids = game.GetValidMoves(board, 1);
                double[] probs = Enumerable.Repeat(1.0 / counts.Length, counts.Length).ToArray();
                for (int i = 0; i < probs.Length; i++)
                {
                    probs[i] *= valids[i] ? 1 : 0;
                }
                double sum_probs = probs.Sum();
                probs = probs.Select(x => x / sum_probs).ToArray();
                return probs;
            }

            double[] probs_normalized = counts.Select(x => x / counts_sum).ToArray();
            return probs_normalized;
        }

        private double search(Board board, int cur_player, int depth = 0)
        {
            int max_depth = args.maxMCTSDepth;
            if (depth > max_depth)
            {
                double draw_value = 0;
                return draw_value;
            }

            string s = game.StringRepresentation(board);

            if (!gameended_state.ContainsKey(s))
            {
                int game_ended = game.GetGameEnded(board, 1);
                gameended_state[s] = game_ended;
            }

            if (gameended_state[s] != 0)
            {
                int game_ended = gameended_state[s];
                visitcount_state[s] = 1;
                return -game_ended;
            }

            if (!policy_for_state.ContainsKey(s))
            {
                double[] policy, v;
                (policy, v) = nnet.Predict(board);
                v = v[0];

                if (depth == 0 && is_training)
                {
                    double epsilon = 0.5;
                    double dirichlet_alpha = 0.5;
                    double[] noise = Enumerable.Repeat(1.0, game.GetActionSize()).Select(x => new Random().NextDouble()).ToArray();
                    noise = noise.Select(x => Math.Pow(x, dirichlet_alpha)).ToArray();
                    noise = noise.Select(x => x / noise.Sum()).ToArray();
                    policy = policy.Select((x, i) => (1 - epsilon) * x + epsilon * noise[i]).ToArray();
                    policy = policy.Select(x => x / policy.Sum()).ToArray();
                }

                bool[] valids = game.GetValidMoves(board, 1);
                policy = policy.Zip(valids, (p, v) => p * (v ? 1 : 0)).ToArray();
                double sum_policy_state = policy.Sum();
                if (sum_policy_state > 0)
                {
                    policy = policy.Select(x => x / sum_policy_state).ToArray();
                }
                else
                {
                    Console.WriteLine("All valid moves were masked, doing a workaround.");
                    policy = policy.Zip(valids, (p, v) => p + (v ? 1 : 0)).ToArray();
                    policy = policy.Select(x => x / policy.Sum()).ToArray();
                }

                policy_for_state[s] = policy;
                validmoves_state[s] = valids;
                visitcount_state[s] = 0;

                return -v;
            }

            bool[] valids = validmoves_state[s];
            double cur_best = double.NegativeInfinity;
            int best_act = -1;

            for (int a = 0; a < game.GetActionSize(); a++)
            {
                if (valids[a])
                {
                    double q_sa, sa_policy;
                    int visit_count_s, visit_count_sa;
                    if (visitcount_stateaction.ContainsKey((s, a)))
                    {
                        q_sa = q_for_stateaction[(s, a)];
                        sa_policy = policy_for_state[s][a];
                        visit_count_s = visitcount_state[s];
                        visit_count_sa = visitcount_stateaction[(s, a)];
                    }
                    else
                    {
                        sa_policy = policy_for_state[s][a];
                        visit_count_s = visitcount_state[s];
                        q_sa = 0;
                    }

                    double u = (q_sa + args.cpuct * sa_policy * Math.Sqrt(visit_count_s) / (1 + visit_count_sa));
                    u = double.IsNaN(u) ? 0 : u;

                    if (u > cur_best)
                    {
                        cur_best = u;
                        best_act = a;
                    }
                }
            }

            int action = best_act;

            if (!visitcount_stateaction.ContainsKey((s, action)))
            {
                visitcount_stateaction[(s, action)] = 1;
            }
            else
            {
                visitcount_stateaction[(s, action)] += 1;
            }

            visitcount_state[s] += 1;
            Board next_s;
            double[] _;
            (next_s, _) = game.GetNextState(board, 1, action);
            next_s = game.TurnBoard(next_s);

            double v = search(next_s, cur_player * -1, depth + 1);

            double val = v;

            if (visitcount_stateaction.ContainsKey((s, action)))
            {
                int visit_count_sa = visitcount_stateaction[(s, action)];
                double q_sa = q_for_stateaction[(s, action)];
                double new_q = (visit_count_sa * q_sa + val) / (visit_count_sa + 1);
                q_for_stateaction[(s, action)] = new_q;
            }
            else
            {
                q_for_stateaction[(s, action)] = val;
            }

            return -v;
        }
    }

    public class Args
    {
        public int numMCTSSims;
        public int numMCTSPlay;
        public int maxMCTSDepth;
        public double cpuct;
    }
}
