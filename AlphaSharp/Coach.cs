using AlphaSharp.Interfaces;
using System;
using System.Collections.Generic;

namespace AlphaSharp
{
    public class Coach
    {
        private readonly List<TrainingData> _trainingData = new ();

        public void Run(IGame game, ISkynet skynet, string tempFolder, Args args)
        {

            for (int iter = 0; iter < args.SelfPlayIterations; iter++)
            {
                Console.WriteLine($"--- starting iteration {iter + 1}/{args.SelfPlayIterations} ---");

                for (int epi = 0; epi < args.SelfPlayEpisodes; epi++)
                {
                    Console.WriteLine($"starting episode {epi + 1}/{args.SelfPlayEpisodes}");

                    var episodeTrainingData = RunEpisode(game, skynet, args);
                    _trainingData.AddRange(episodeTrainingData);

                    // remove oldest examples first if over the limit
                    if (_trainingData.Count > args.TrainingMaxExamples)
                        _trainingData.RemoveRange(0, _trainingData.Count - args.TrainingMaxExamples);
                }

                Train();
                SelfPlay();
            }
            // add output training examples from all episodes to global training set.
            // train skynet on global training set.
            // run self play to accept or reject new model.
        }

        private List<TrainingData> RunEpisode(IGame game, ISkynet skynet, Args args)
        {
            var state = new byte[game.StateSize];
            var actions = new byte[game.ActionCount];
            var mcts = new Mcts(game, skynet, args);

            var trainingData = new List<TrainingData>();

            game.SetStartingState(state);

            int moves = 0;
            float currentPlayer = 1;
            int gameResult;

            while (true)
            {
                if (moves++ >= args.TrainingEpisodeMaxMoves)
                {
                    gameResult = 0;
                    break;
                }

                game.GetValidActions(state, actions);
                var probs = mcts.GetActionProbs(state, isTraining: true);
                var sym = game.GetStateSymmetries(state, probs);
                foreach (var s in sym)
                    trainingData.Add(new TrainingData(s.Item1, s.Item2, currentPlayer));

                trainingData.Add(new TrainingData(state, probs, currentPlayer));

                int selectedAction = ArrayUtil.ArgMax(probs);
                game.ExecutePlayerAction(state, selectedAction);

                gameResult = game.GetGameEnded(state);
                if (gameResult != 0)
                    break;

                game.FlipStateToNextPlayer(state);

                currentPlayer *= -1;
            }

            Console.WriteLine("episode result: " + gameResult * currentPlayer + ", moves: " + (moves - 1));
            // adjust training with the final game result
            for (int i = 0; i < trainingData.Count; i++)
            {
                // gameResult is 0 (draw) or 1 (since board is seen from player 1's perspective when moving).
                // so the actual result is gained by just multiplying with the value of currentPlayer which is either 1 or -1.
                trainingData[i].Player1Value = gameResult * trainingData[i].Player1Value * -1;
            }

            return trainingData;
        }

        private void Train()
        {
            // train skynet on global training set.
        }

        private void SelfPlay()
        {
            // play x time against previous model. In parallel!
            // return result (win/loss/draw).
        }
    }
}
