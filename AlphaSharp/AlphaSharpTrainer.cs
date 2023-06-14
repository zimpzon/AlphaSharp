﻿using AlphaSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace AlphaSharp
{
    public class AlphaSharpTrainer
    {
        private List<TrainingData> _trainingSamples = new();
        private readonly object _lock = new();
        private readonly IGame _game;
        private readonly ISkynet _skynet;
        private readonly Func<ISkynet> _skynetCreator;
        private readonly AlphaParameters _param;

        private readonly string _filenameBestSkynet = "{NAME}-best.skynet";
        private readonly string _filenamePreTrainingSkynet = "{NAME}-pre-training.skynet";
        private readonly string _filenameTrainingSamplesLatest = "{NAME}-training-samples-latest.json";

        private int _iteration;

        private class EpisodeParam
        {
            public ProgressInfo Progress { get; set; }
        }

        private class EvaluationParam
        {
            public ProgressInfo Progress { get; set; }
            public int Round { get; set; }
            public ISkynet OldSkynet { get; set; }
        }

        public AlphaSharpTrainer(IGame game, Func<ISkynet> skynetCreator, AlphaParameters param)
        {
            _game = game;
            _skynetCreator = skynetCreator;
            _param = param;

            _filenameBestSkynet = _filenameBestSkynet.Replace("{NAME}", _game.Name);
            _filenamePreTrainingSkynet = _filenamePreTrainingSkynet.Replace("{NAME}", _game.Name);
            _filenameTrainingSamplesLatest = _filenameTrainingSamplesLatest.Replace("{NAME}", _game.Name);

            _skynet = skynetCreator();

            if (!Directory.Exists(param.OutputFolder))
            {
                param.TextInfoCallback(LogLevel.Info, $"Creating output folder {param.OutputFolder}");
                Directory.CreateDirectory(param.OutputFolder);
            }   
        }

        public void Run()
        {
            if (_param.ResumeFromCheckpoint)
            {
                _param.TextInfoCallback(LogLevel.Info, "Resuming from latest checkpoint...");

                var trainingSamplesPath = Path.Combine(_param.OutputFolder, _filenameTrainingSamplesLatest);
                if (File.Exists(trainingSamplesPath))
                {
                    _trainingSamples = JsonSerializer.Deserialize<List<TrainingData>>(File.ReadAllText(trainingSamplesPath));
                    _param.TextInfoCallback(LogLevel.Info, $"{_trainingSamples.Count} training samples loaded from {trainingSamplesPath}");
                }
                else
                {
                    _param.TextInfoCallback(LogLevel.Info, "no existing training samples found");
                }

                var skynetModelFile = Path.Combine(_param.OutputFolder, _filenameBestSkynet);
                if (File.Exists(skynetModelFile))
                {
                    _skynet.LoadModel(skynetModelFile);
                    _param.TextInfoCallback(LogLevel.Info, $"best skynet model loaded from {skynetModelFile}");
                }
                else
                {
                    _param.TextInfoCallback(LogLevel.Info, "no existing skynet model found");
                }
            }

            var iterationProgress = ProgressInfo.Create(ProgressInfo.Phase.Iteration, _param.Iterations);
            _param.TextInfoCallback(LogLevel.Info, "");
            _param.TextInfoCallback(LogLevel.Info, $"Starting {_param.Iterations} iterations of [self-play / train / evaluate] cycle of game {_game.Name}");

            for (int iter = 0; iter < _param.Iterations; iter++)
            {
                _iteration = iter;

                _param.ProgressCallback(iterationProgress.Update(iter + 1), string.Empty);

                // Self-play episodes
                var episodeProgress = ProgressInfo.Create(ProgressInfo.Phase.SelfPlay, _param.SelfPlayEpisodes);

                var episodeNumbers = Enumerable.Range(0, _param.SelfPlayEpisodes).ToList();
                var episodeParam = episodeNumbers.Select(e => new EpisodeParam { Progress = episodeProgress }).ToList();
                var consumer = new ThreadedConsumer<EpisodeParam, List<TrainingData>>(RunEpisode, _param.MaxWorkerThreads);

                _param.TextInfoCallback(LogLevel.Info, "");
                _param.TextInfoCallback(LogLevel.Info, $"Starting {_param.SelfPlayEpisodes} episodes of self-play using {_param.MaxWorkerThreads} worker thread{(_param.MaxWorkerThreads == 1 ? "" : "s")}");

                episodesCompleted = 0;
                var episodesTrainingData = consumer.Run(episodeParam);

                var newSamples = episodesTrainingData.SelectMany(e => e).ToList();

                _param.TextInfoCallback(LogLevel.MoreInfo, $"Self-play added {newSamples.Count} new samples of training data");

                _trainingSamples.AddRange(newSamples);

                if (_trainingSamples.Count > _param.MaxTrainingExamples)
                {
                    int removeCount = _trainingSamples.Count - _param.MaxTrainingExamples;
                    _param.TextInfoCallback(LogLevel.MoreInfo, $"Limit of {_param.MaxTrainingExamples} training examples reached, removing {removeCount} oldest samples");
                    _trainingSamples.RemoveRange(0, removeCount);
                }

                _param.TextInfoCallback(LogLevel.Info, "");
                _param.TextInfoCallback(LogLevel.Info, $"Starting training with {_trainingSamples.Count} samples of training data");
                Train(_trainingSamples);
                _param.TextInfoCallback(LogLevel.Info, $"Training complete");

                _param.TextInfoCallback(LogLevel.Info, "");
                string msg = $"Comparing new model to old model in {_param.EvaluationRounds} rounds using {_param.MaxWorkerThreads} worker thread{(_param.MaxWorkerThreads == 1 ? "" : "s")}";
                _param.TextInfoCallback(LogLevel.Info, msg);

                EvaluateNewModel();
            }
        }

        int episodesCompleted = 0;

        private List<TrainingData> RunEpisode(EpisodeParam param)
        {
            var state = new byte[_game.StateSize];
            var mcts = new Mcts(_game, _skynet, _param);

            var trainingData = new List<TrainingData>();

            _game.GetStartingState(state);

            float currentPlayer = 1;
            GameOver.Status gameResult;

            var rnd = new Random();

            var prevState = new byte[_game.StateSize];
            var validActions = new byte[_game.ActionCount];
            int moves = 0;

            while (true)
            {
                _game.GetValidActions(state, validActions);

                float temperature = moves > _param.TemperatureThresholdMoves ? 0.1f : 1.0f;
                var probs = mcts.GetActionProbsForSelfPlay(state, temperature);
                ArrayUtil.FilterProbsByValidActions(probs, validActions);
                ArrayUtil.Normalize(probs);

                trainingData.Add(new TrainingData(state, probs, currentPlayer));

                // Skipping symmetries for now
                //var sym = _game.GetStateSymmetries(state, probs);
                //foreach (var s in sym)
                //    trainingData.Add(new TrainingData(s.Item1, s.Item2, currentPlayer));

                int selectedAction = ArrayUtil.WeightedChoice(rnd, probs);
                _game.ExecutePlayerAction(state, selectedAction);
                moves++;

                gameResult = _game.GetGameEnded(state, moves, isSimulation: false);
                if (gameResult != GameOver.Status.GameIsNotOver)
                    break;

                _game.FlipStateToNextPlayer(state);
                Array.Copy(state, prevState, state.Length);

                currentPlayer *= -1;
            }

            // moves are always done by player1 so invert result if current player is actually player2
            float value = GameOver.ValueForPlayer1(gameResult);
            if (value != 0)
                value *= currentPlayer;

            for (int i = 0; i < trainingData.Count; i++)
                trainingData[i].ValueForPlayer1 *= value;

            lock (_lock)
            {
                episodesCompleted++;
                _param.ProgressCallback(param.Progress.Update(episodesCompleted), string.Empty);
            }

            return trainingData;
        }

        long winNew = 0;
        long winOld = 0;
        long draw = 0;
        long stoppedEarly = 0;
        int evalRoundsCompleted = 0;
        const int NotUsed = 0;

        private int RunEvalRound(EvaluationParam param)
        {
            if (Interlocked.Read(ref stoppedEarly) > 0)
                return NotUsed;

            // Is result decided yet?
            long roundsLeft = _param.EvaluationRounds - (winOld + winNew + draw);
            long roundsToCatchUp = Math.Abs(winOld - winNew);
            if (roundsToCatchUp > roundsLeft)
            {
                Interlocked.Increment(ref stoppedEarly);
                _param.TextInfoCallback(LogLevel.Info, $"Outcome is determined, stopping evaluation early");
                return NotUsed;
            }

            var mctsPlayerOld = new MctsPlayer(_game, param.OldSkynet, _param);
            var mctsPlayerNew = new MctsPlayer(_game, _skynet, _param);

            IPlayer player1 = null;
            IPlayer player2 = null;
            if (_param.EvaluationPlayers == EvaluationPlayers.NewModelAlwaysPlayer1)
            {
                player1 = mctsPlayerNew;
                player2 = mctsPlayerOld;
            }
            else if (_param.EvaluationPlayers == EvaluationPlayers.NewModelAlwaysPlayer2)
            {
                player1 = mctsPlayerOld;
                player2 = mctsPlayerNew;
            }
            else if (_param.EvaluationPlayers == EvaluationPlayers.AlternatingModels)
            {
                player1 = param.Round % 2 == 0 ? mctsPlayerNew : mctsPlayerOld;
                player2 = param.Round % 2 == 0 ? mctsPlayerOld : mctsPlayerNew;
            }

            if (Interlocked.Read(ref stoppedEarly) > 0)
                return NotUsed;

            var oneVsOne = new OneVsOne(_game, player1, player2);
            var gameResult = oneVsOne.Run();

            if (Interlocked.Read(ref stoppedEarly) > 0)
                return NotUsed;

            string resultStr;
            if (gameResult == GameOver.Status.Draw)
            {
                resultStr = "draw";
                Interlocked.Increment(ref draw);
            }
            else
            {
                bool newWon = player1 == mctsPlayerNew && gameResult == GameOver.Status.Player1Won ||
                              player2 == mctsPlayerNew && gameResult == GameOver.Status.Player2Won;

                if (newWon)
                {
                    resultStr = "new model won";
                    Interlocked.Increment(ref winNew);
                }
                else
                {
                    resultStr = "old model won";
                    Interlocked.Increment(ref winOld);
                }
            }

            lock (_lock)
            {
                evalRoundsCompleted++;

                string additionalInfo = $"Round done ({resultStr}), new model: {winNew}, old model: {winOld}, draw: {draw}";
                _param.ProgressCallback(param.Progress.Update(evalRoundsCompleted), additionalInfo);
            }

            return NotUsed;
        }

        private void EvaluateNewModel()
        {
            winNew = 0;
            winOld = 0;
            draw = 0;
            stoppedEarly = 0;

            var oldSkynet = _skynetCreator();
            string oldModelPath = Path.Combine(_param.OutputFolder, _filenamePreTrainingSkynet);

            _param.TextInfoCallback(LogLevel.MoreInfo, $"Loading old model from {oldModelPath}");
            oldSkynet.LoadModel(oldModelPath);

            var progress = ProgressInfo.Create(ProgressInfo.Phase.Eval, _param.EvaluationRounds);

            var countNumbers = Enumerable.Range(0, _param.EvaluationRounds).ToList();
            var evalParam = countNumbers.Select(e => new EvaluationParam {
                Round = e,
                OldSkynet = oldSkynet,
                Progress = progress
            }).ToList();

            var consumer = new ThreadedConsumer<EvaluationParam, int>(RunEvalRound, _param.MaxWorkerThreads);

            evalRoundsCompleted = 0;
            var episodesTrainingData = consumer.Run(evalParam);
            string score = $"new: {winNew}, old: {winOld}, draw: {draw}";

            bool newIsBetter = winNew > winOld;
            if (newIsBetter)
            {
                _param.TextInfoCallback(LogLevel.Info, "");

                string bestModelPath = Path.Combine(_param.OutputFolder, _filenameBestSkynet);
                _param.TextInfoCallback(LogLevel.Info, $"--- New model is BETTER ({score}), keeping it as {bestModelPath} ---");
                _skynet.SaveModel(bestModelPath);

                _param.TextInfoCallback(LogLevel.Info, "");
            }
            else
            {
                _param.TextInfoCallback(LogLevel.Info, "");

                string prevModelPath = Path.Combine(_param.OutputFolder, _filenamePreTrainingSkynet);
                _param.TextInfoCallback(LogLevel.Info, $"--- New model is NOT better ({score}), reloading previous model at {prevModelPath} ---");
                _skynet.LoadModel(prevModelPath);

                _param.TextInfoCallback(LogLevel.Info, "");
            }

            if (_param.SaveBackupAfterIteration)
            {
                if (newIsBetter)
                {
                    string backupModelPath = Path.Combine(_param.OutputFolder, $"{_filenameBestSkynet}.{_iteration}.backup");
                    _param.TextInfoCallback(LogLevel.Info, $"Saving backup of model at {backupModelPath}");
                    _skynet.SaveModel(backupModelPath);
                }

                string backupTrainingDataPath = Path.Combine(_param.OutputFolder, $"{_filenameTrainingSamplesLatest}.{_iteration}.backup");
                _param.TextInfoCallback(LogLevel.Info, $"Saving backup of training data at {backupTrainingDataPath}");
                string json = JsonSerializer.Serialize(_trainingSamples);
                File.WriteAllText(backupTrainingDataPath, json);

                _param.TextInfoCallback(LogLevel.Info, "");
            }
        }

        public delegate void TrainingProgressCallback(int currentValue, int numberOfValues, string additionalInfo = null);

        private void Train(List<TrainingData> trainingData)
        {
            string trainingSamplesPath = Path.Combine(_param.OutputFolder, _filenameTrainingSamplesLatest);
            _param.TextInfoCallback(LogLevel.MoreInfo, $"Saving current training samples ({_trainingSamples.Count}) to {trainingSamplesPath}");

            string json = JsonSerializer.Serialize(_trainingSamples);
            File.WriteAllText(trainingSamplesPath, json);

            string oldModelPath = Path.Combine(_param.OutputFolder, _filenamePreTrainingSkynet);
            _param.TextInfoCallback(LogLevel.MoreInfo, $"Saving skynet model before training to {oldModelPath}");
            _skynet.SaveModel(oldModelPath);

            var trainingProgress = ProgressInfo.Create(ProgressInfo.Phase.Train);

            void ProgressCallback(int currentValue, int numberOfValues, string additionalInfo = null)
            {
                if (!string.IsNullOrWhiteSpace(additionalInfo))
                    _param.TextInfoCallback(LogLevel.MoreInfo, $"Info from Skynet: {additionalInfo}");

                _param.ProgressCallback(trainingProgress.Update(currentValue, numberOfValues), additionalInfo);
            }

            var callback = new TrainingProgressCallback(ProgressCallback);

            _skynet.Train(trainingData, callback);
        }
    }
}
