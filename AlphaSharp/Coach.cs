using AlphaSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AlphaSharp
{
    public class Coach
    {
        private List<TrainingData> _trainingData = new();

        public void Run(IGame game, ISkynet skynet, ISkynet evaluationSkynet, string tempFolder, Args args)
        {
            if (args.ResumeFromCheckpoint)
            {
                Console.WriteLine("Loading trainingdata...");
                if (File.Exists("c:\\temp\\zerosharp\\tixy-training-data-latest.json"))
                    _trainingData = JsonSerializer.Deserialize<List<TrainingData>>(File.ReadAllText("c:\\temp\\zerosharp\\tixy-training-data-latest.json"));
                else
                    Console.WriteLine("No existing trainingdata found");
            }

            for (int iter = 0; iter < args.Iterations; iter++)
            {
                Console.WriteLine($"--- starting iteration {iter + 1}/{args.Iterations} ---");

                // Self-play episodes
                for (int epi = 0; epi < args.TrainSelfPlayEpisodes; epi++)
                {
                    Console.WriteLine($"starting episode {epi + 1}/{args.TrainSelfPlayEpisodes}");

                    var episodeTrainingData = RunEpisode(game, skynet, args);
                    _trainingData.AddRange(episodeTrainingData);

                    // remove oldest examples first if over the limit
                    if (_trainingData.Count > args.TrainingMaxExamples)
                        _trainingData.RemoveRange(0, _trainingData.Count - args.TrainingMaxExamples);
                }

                Train(_trainingData, skynet, args, iter);

                EvaluateModel(game, skynet, evaluationSkynet, args);
            }
        }

        private void EvaluateModel(IGame game, ISkynet skynet, ISkynet evaluationSkynet, Args args)
        {
            int newWon = 0;
            int oldWon = 0;
            int evalDraw = 0;
            evaluationSkynet.LoadModel("c:\\temp\\zerosharp\\tixy-model-pre-train-latest.pt");

            for (int i = 0; i < 10; ++i)
            {
                var mctsPlayerOld = new MctsPlayer(game, evaluationSkynet, args);
                var mctsPlayerNew = new MctsPlayer(game, skynet, args);
                var oneVsOne = new OneVsOne(game, mctsPlayerNew, mctsPlayerOld);
                int result = oneVsOne.Run(args.EvalSimulationMaxMoves);
                if (result == 0)
                    evalDraw++;
                else if (result == 1)
                    newWon++;
                else if (result == -1)
                    oldWon++;

                Console.WriteLine($"new vs old model: newWon: {newWon}, oldWon: {oldWon}, draw: {evalDraw}");
            }

            bool newIsBetter = newWon > oldWon;
            if (newIsBetter)
            {
                Console.WriteLine("new model is better, keeping it");
            }
            else
            {
                Console.WriteLine("old model is better, dropping new model");
                skynet.LoadModel("c:\\temp\\zerosharp\\tixy-model-pre-train-latest.pt");
            }

            //int oneWon = 0;
            //int twoWon = 0;
            //int draw = 0;

            //for (int i = 0; i < 10; ++i)
            //{
            //    var randomPlayer = new RandomPlayer(game);
            //    var mctsPlayer = new MctsPlayer(game, skynet, args);
            //    var oneVsOne = new OneVsOne(game, randomPlayer, mctsPlayer);
            //    int result = oneVsOne.Run(args.EvalSimulationMaxMoves);
            //    if (result == 0)
            //        draw++;
            //    else if (result == 1)
            //        oneWon++;
            //    else if (result == -1)
            //        twoWon++;

            //    Console.WriteLine($"AI as player2: aiWon: {oneWon}, randomWon: {twoWon}, draw: {draw}");
            //}

            //for (int i = 0; i < 10; ++i)
            //{
            //    var randomPlayer = new RandomPlayer(game);
            //    var mctsPlayer = new MctsPlayer(game, skynet, args);
            //    var oneVsOne = new OneVsOne(game, mctsPlayer, randomPlayer);
            //    int result = oneVsOne.Run(args.EvalSimulationMaxMoves);
            //    if (result == 0)
            //        draw++;
            //    else if (result == 1)
            //        oneWon++;
            //    else if (result == -1)
            //        twoWon++;

            //    Console.WriteLine($"AI as player1: aiWon: {oneWon}, randomWon: {twoWon}, draw: {draw}");
            //}
        }

        private void Train(List<TrainingData> trainingData, ISkynet skynet, Args args, int iteration)
        {
            skynet.Train(trainingData, args, iteration);
        }

        private List<TrainingData> RunEpisode(IGame game, ISkynet skynet, Args args)
        {
            var state = new byte[game.StateSize];
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

                var probs = mcts.GetActionProbs(state, isTraining: true);

                var sym = game.GetStateSymmetries(state, probs);
                foreach (var s in sym)
                    trainingData.Add(new TrainingData(s.Item1, s.Item2, currentPlayer));

                trainingData.Add(new TrainingData(state, probs, currentPlayer));

                int selectedAction = ArrayUtil.ArgMax(probs);
                game.ExecutePlayerAction(state, selectedAction);

                gameResult = game.GetGameEnded(state);
                if (gameResult != 0)
                {
                    Console.Write("end state:");
                    game.PrintState(state, Console.WriteLine);
                    Console.Write("winning move: ");
                    game.PrintDisplayTextForAction(selectedAction, Console.WriteLine);
                    break;
                }

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
    }
}
