using AlphaSharp;
using TixyGame;
using TorchSharp;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run()
        {
            // +progress callback
            // progress print helpers
            // tic tac from py
            // subfolders in project
            // beautiful progress, something. with stats for all iterations.

            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 4, // diminishing returns, 4 threads seems optimal'ish on home pc with 12/24 cores
                MaxTrainingExamples = 100000,
                Cpuct = 1.0f,
                OutputFolder = "c:\\temp\\zerosharp\\tixy",

                // self-play
                SelfPlaySimulationCount = 500,
                SelfPlaySimulationMaxMoves = 500,
                SelfPlayEpisodeMaxMoves = 150,
                SelfPlayEpisodes = 20,

                // evaluation
                EvaluationRounds = 10,
                EvaluationSimulationCount = 1000,
                EvaluationSimulationMaxMoves = 500,
                EvaluationMaxMoves = 150,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 64,
                TrainingLearningRate = 0.001f,
            };

            var game = new Tixy(5, 5);
            var skynetCreator = () => new TixySkynet(game, tixyParam);
            var skynet = new TixySkynet(game, tixyParam);
            var evaluationSkynet = new TixySkynet(game, tixyParam);

            var trainer = new AlphaSharpTrainer(game, skynetCreator, alphaParam);
            trainer.Run();
        }

        static void Main(string[] _)
        {
            //while (true)
            {
                try
                {
                    Run();
                    Console.WriteLine("Done");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}