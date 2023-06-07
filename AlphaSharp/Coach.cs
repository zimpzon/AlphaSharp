using AlphaSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace AlphaSharp
{
    public class Coach
    {
        private List<TrainingData> _trainingData = new();
        private int _modelsKept = 0;
        private int _modelsDropped = 0;
        private readonly object _lock = new();
        long episodesCompleted = 0;


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
                episodesCompleted = 0;

                var episodeNumbers = Enumerable.Range(0, args.selfPlayEpisodes).ToList();
                var episodeParam = episodeNumbers.Select(e => new EpisodeParam { Game = game, Skynet = skynet, Args = args, Episode = e }).ToList();
                var consumer = new ThreadedConsumer<EpisodeParam, List<TrainingData>>(RunEpisode, args.MaxWorkerThreads);

                var sw = Stopwatch.StartNew();

                var episodesTrainingData = consumer.Run(episodeParam);

                var elapsed = sw.Elapsed;
                var newSamples = episodesTrainingData.SelectMany(e => e).ToList();

                Console.WriteLine($"completed {args.selfPlayEpisodes} episodes in {elapsed.TotalSeconds:0.00} sec (avg: {elapsed.TotalSeconds / args.selfPlayEpisodes:0.00} sec), samples collected: {newSamples.Count}");

                _trainingData.AddRange(newSamples);

                // remove oldest examples first if over the limit
                if (_trainingData.Count > args.SelfPlayMaxExamples)
                {
                    Console.WriteLine($"removing {_trainingData.Count - args.SelfPlayMaxExamples} oldest trainingData examples");
                    _trainingData.RemoveRange(0, _trainingData.Count - args.SelfPlayMaxExamples);
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

            for (int i = 0; i < 20; ++i)
            {
                bool newFirst = i % 2 == 0;

                var mctsPlayerOld = new MctsPlayer(game, evaluationSkynet, args);
                var mctsPlayerNew = new MctsPlayer(game, skynet, args);
                var oneVsOne = new OneVsOne(game, newFirst ? mctsPlayerNew : mctsPlayerOld, newFirst ? mctsPlayerOld : mctsPlayerNew);
                int result = oneVsOne.Run(args.EvalSimulationMaxMoves);

                int winValueNew = newFirst ? 1 : -1;
                int winValueOld = newFirst ? -1 : 1;

                if (result == 0)
                    evalDraw++;
                else if (result == winValueOld)
                    oldWon++;
                else if (result == winValueNew)
                    newWon++;

                if (oldWon >= 10 || newWon > 10)
                {
                    Console.WriteLine($"winner decided early: newWon: {newWon}, oldWon: {oldWon}, draw: {evalDraw}");
                    break;
                }

                string s = newFirst ? "new as p1" : "old as p1";

                Console.WriteLine($"score ({s}): newWon: {newWon}, oldWon: {oldWon}, draw: {evalDraw}");
            }

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
        }

        private static void Train(List<TrainingData> trainingData, ISkynet skynet, Args args, int iteration)
        {
            skynet.Train(trainingData, args, iteration);
        }

        long concurrentEpisodes = 0;

        private List<TrainingData> RunEpisode(EpisodeParam param)
        {
            Interlocked.Increment(ref concurrentEpisodes);

            var game = param.Game;
            var skynet = param.Skynet;
            var args = param.Args;

            var state = new byte[game.StateSize];
            var mcts = new Mcts(game, skynet, args);

            var trainingData = new List<TrainingData>();

            game.SetStartingState(state);

            int moves = 0;
            float currentPlayer = 1;
            float gameResult;

            var rnd = new Random();

            var prevState = new byte[game.StateSize];

            while (true)
            {
                if (moves++ > args.SelfPlayEpisodeMaxMoves)
                {
                    // a value of 0 will f the loss calculation up since it does a multiply with value.
                    // so use a small non-zero value instead.
                    gameResult = 0.0001f;

                    // just keep a few samples from draws, the ones in the end probably caused the draw
                    trainingData = trainingData.TakeLast(20).ToList();
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

                    //Console.Write("last move: ");
                    //game.PrintDisplayTextForAction(selectedAction, Console.WriteLine);
                    //Console.Write("from state: ");
                    //game.PrintState(prevState, Console.WriteLine);
                    //Console.Write("to state: ");
                    //game.PrintState(state, Console.WriteLine);

                    break;
                }

                game.FlipStateToNextPlayer(state);
                Array.Copy(state, prevState, state.Length);

                currentPlayer *= -1;
            }

            double msPerSkynetCall = mcts.Stats.MsInSkynet / mcts.Stats.SkynetCalls;

            lock (_lock)
            {
                episodesCompleted++;
                Console.WriteLine($"episode result {episodesCompleted}/{args.selfPlayEpisodes}: {gameResult}, moves: {moves}, skynet/ms: {msPerSkynetCall:0.000}, {mcts.Stats}, concurrentEpisodes: {concurrentEpisodes}");
            }

            for (int i = 0; i < trainingData.Count; i++)
                trainingData[i].Player1Value *= gameResult;

            Interlocked.Decrement(ref concurrentEpisodes);

            return trainingData;
        }
    }
}
