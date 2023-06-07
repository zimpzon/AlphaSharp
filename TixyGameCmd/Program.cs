using AlphaSharp;
using TixyGame;
using TorchSharp;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run()
        {
            var args = new Args
            {
                ResumeFromCheckpoint = true,
                ResumeFromEval = false,
                Iterations = 1000,
                MaxWorkerThreads = 4, // 4 threads seems optimal'ish on home pc with 12/24 cores // Environment.ProcessorCount,

                // self-play
                SelfPlaySimulationCount = 2000,
                SelfPlaySimulationMaxMoves = 500,
                SelfPlayEpisodeMaxMoves = 150,
                selfPlayEpisodes = 40,
                
                // net training
                TrainingEpochs = 10,
                TrainingLearningRate = 0.001f,
                TrainingBatchSize = 64,
                SelfPlayMaxExamples = 100000,
                Cpuct = 1.0f,

                // evaluation
                EvalRounds = 20,
                EvalSimulationCount = 1000,
                EvalSimulationMaxMoves = 500,
                EvalMaxMoves = 150,
            };

            //var args = new Args
            //{
            //    ResumeFromCheckpoint = true,
            //    ResumeFromEval = false,
            //    Iterations = 1000,
            //    MaxWorkerThreads = 1, // Environment.ProcessorCount,

            //    // self-play
            //    SelfPlaySimulationCount = 2000,
            //    SelfPlaySimulationMaxMoves = 500,
            //    SelfPlayEpisodeMaxMoves = 150,
            //    selfPlayEpisodes = 30,

            //    // net training
            //    TrainingEpochs = 10,
            //    TrainingLearningRate = 0.001f,
            //    TrainingBatchSize = 64,
            //    SelfPlayMaxExamples = 100000,
            //    Cpuct = 1.0f,

            //    // evaluation
            //    EvalSimulationCount = 1000,
            //    EvalSimulationMaxMoves = 300,
            //    EvalMaxMoves = 150,
            //};

            var game = new Tixy(7, 7);
            var skynet = new TixySkynet(game, args);
            var evaluationSkynet = new TixySkynet(game, args);
            var coach = new Coach();
            coach.Run(game, skynet, evaluationSkynet, args);
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