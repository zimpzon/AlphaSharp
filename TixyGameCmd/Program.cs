using AlphaSharp;
using TixyGame;
using TorchSharp;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run()
        {
            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                ResumeFromEval = false,
                Iterations = 1000,
                MaxWorkerThreads = 4, // diminishing returns, 4 threads seems optimal'ish on home pc with 12/24 cores
                MaxTrainingExamples = 100000,
                Cpuct = 1.0f,
                OutputFolder = "c:\\temp\\zerosharp\\tixy",

                // self-play
                SelfPlaySimulationCount = 2000,
                SelfPlaySimulationMaxMoves = 500,
                SelfPlayEpisodeMaxMoves = 150,
                SelfPlayEpisodes = 40,

                // evaluation
                EvaluationRounds = 20,
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
            var skynet = new TixySkynet(game, tixyParam);
            var evaluationSkynet = new TixySkynet(game, tixyParam);
            var coach = new Coach();
            coach.Run(game, skynet, evaluationSkynet, alphaParam);
        }

        static void Main(string[] _)
        {
            torch.set_num_threads(1);
            torch.set_num_interop_threads(1);

            while (true)
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