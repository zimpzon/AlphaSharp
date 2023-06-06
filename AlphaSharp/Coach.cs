using AlphaSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AlphaSharp
{
    public class Coach
    {
        private List<TrainingData> _trainingData = new();
        private int _modelsKept = 0;
        private int _modelsDropped = 0;

        public void Run(IGame game, ISkynet skynet, ISkynet evaluationSkynet, Args args)
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
                if (args.ResumeFromEval && iter == 0)
                {
                    Console.WriteLine("Skipping training and evaluating existing model...");
                    EvaluateModel(game, skynet, evaluationSkynet, args);
                    continue;
                }

                Console.WriteLine($"--- starting iteration {iter + 1}/{args.Iterations} ---");

                // Self-play episodes
                for (int epi = 0; epi < args.selfPlayEpisodes; epi++)
                {
                    //Console.WriteLine($"starting episode {epi + 1}/{args.TrainSelfPlayEpisodes}");

                    var episodeTrainingData = RunEpisode(game, skynet, args);
                    _trainingData.AddRange(episodeTrainingData);

                    // remove oldest examples first if over the limit
                    if (_trainingData.Count > args.SelfPlayMaxExamples)
                    {
                        Console.WriteLine($"removing {_trainingData.Count - args.SelfPlayMaxExamples} oldest trainingData examples");
                        _trainingData.RemoveRange(0, _trainingData.Count - args.SelfPlayMaxExamples);
                    }
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

            Console.WriteLine("evaluating new model against previous model...");

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
            }

            Console.WriteLine($"new as player 1: newWon: {newWon}, oldWon: {oldWon}, draw: {evalDraw}");

            for (int i = 0; i < 10; ++i)
            {
                var mctsPlayerOld = new MctsPlayer(game, evaluationSkynet, args);
                var mctsPlayerNew = new MctsPlayer(game, skynet, args);
                var oneVsOne = new OneVsOne(game, mctsPlayerOld, mctsPlayerNew);
                int result = oneVsOne.Run(args.EvalSimulationMaxMoves);
                if (result == 0)
                    evalDraw++;
                else if (result == 1)
                    oldWon++;
                else if (result == -1)
                    newWon++;
            }

            Console.WriteLine($"total: newWon: {newWon}, oldWon: {oldWon}, draw: {evalDraw}");

            bool newIsBetter = newWon > oldWon;
            if (newIsBetter)
            {
                _modelsKept++;
                Console.WriteLine($"new model is better, keeping it (kept: {_modelsKept}/{_modelsDropped + _modelsKept})");
                skynet.SaveModel("c:\\temp\\zerosharp\\tixy-model-best.pt");
            }
            else
            {
                _modelsDropped++;
                Console.WriteLine($"new model is NOT better, dropping new model (kept: {_modelsKept}/{_modelsDropped + _modelsKept})");
                skynet.LoadModel("c:\\temp\\zerosharp\\tixy-model-pre-train-latest.pt");
            }

            int oneWon = 0;
            int twoWon = 0;
            int draw = 0;

            for (int i = 0; i < 10; ++i)
            {
                var randomPlayer = new RandomPlayer(game);
                var mctsPlayer = new MctsPlayer(game, skynet, args);
                var oneVsOne = new OneVsOne(game, randomPlayer, mctsPlayer);
                int result = oneVsOne.Run(args.EvalSimulationMaxMoves);
                if (result == 0)
                    draw++;
                else if (result == 1)
                    oneWon++;
                else if (result == -1)
                    twoWon++;
            }
            Console.WriteLine($"AI as player2: aiWon: {twoWon}, randomWon: {oneWon}, draw: {draw}");

            oneWon = 0;
            twoWon = 0;
            draw = 0;

            for (int i = 0; i < 10; ++i)
            {
                var randomPlayer = new RandomPlayer(game);
                var mctsPlayer = new MctsPlayer(game, skynet, args);
                var oneVsOne = new OneVsOne(game, mctsPlayer, randomPlayer);
                int result = oneVsOne.Run(args.EvalSimulationMaxMoves);
                if (result == 0)
                    draw++;
                else if (result == 1)
                    oneWon++;
                else if (result == -1)
                    twoWon++;
            }
            Console.WriteLine($"AI as player1: aiWon: {oneWon}, randomWon: {twoWon}, draw: {draw}");
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
            float gameResult;

            var rnd = new Random();

            while (true)
            {
                if (moves++ > args.SelfPlayEpisodeMaxMoves)
                {
                    // a value of 0 will f the loss calculation up since it does a multiply with value.
                    // so use a small non-zero value instead.
                    gameResult = 0.0001f;

                    // just keep a few samples from draws so we never risk 0 samples
                    trainingData = trainingData.Take(10).ToList();
                    break;
                }

                var probs = mcts.GetActionProbs(state, isSelfPlay: true);

                var sym = game.GetStateSymmetries(state, probs);
                foreach (var s in sym)
                    trainingData.Add(new TrainingData(s.Item1, s.Item2, currentPlayer));

                int selectedAction = ArrayUtil.WeightedChoice(rnd, probs);
                game.ExecutePlayerAction(state, selectedAction);

                //game.PrintState(state, Console.WriteLine);

                gameResult = game.GetGameEnded(state);
                if (gameResult != 0)
                {
                    gameResult = currentPlayer;
                    //Console.Write("end state:");
                    //game.PrintState(state, Console.WriteLine);
                    //Console.Write("winning move: ");
                    //game.PrintDisplayTextForAction(selectedAction, Console.WriteLine);
                    break;
                }

                game.FlipStateToNextPlayer(state);

                currentPlayer *= -1;
            }

            Console.WriteLine("episode result: " + gameResult + ", moves: " + moves);

            for (int i = 0; i < trainingData.Count; i++)
            {
                trainingData[i].Player1Value *= gameResult;
            }

            return trainingData;
        }
    }
}
