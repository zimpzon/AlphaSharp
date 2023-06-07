using AlphaSharp;
using TixyGame;
using TorchSharp;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run(int id)
        {
            var args = new Args
            {
                ResumeFromCheckpoint = true,
                ResumeFromEval = false,
                Iterations = 1000,

                // self-play
                SelfPlaySimulationCount = 2000,
                SelfPlaySimulationMaxMoves = 500,
                SelfPlayEpisodeMaxMoves = 150,
                selfPlayEpisodes = 30,
                
                // net training
                TrainingEpochs = 10,
                TrainingLearningRate = 0.001f,
                TrainingBatchSize = 64,
                SelfPlayMaxExamples = 100000,
                Cpuct = 1.0f,

                // evaluation
                EvalSimulationCount = 1000,
                EvalSimulationMaxMoves = 300,
                EvalMaxMoves = 150,
            };

            //args = new Args
            //{
            //    // self-play
            //    TrainingSimulationCount = 10,
            //    TrainingSimulationMaxMoves = 10,
            //    TrainingEpisodeMaxMoves = 10,

            //    // net training
            //    TrainEpochs = 10,
            //    TrainLearningRate = 0.001f,
            //    TrainPlayEpisodes = 10,

            //    // evaluation
            //    EvalSimulationCount = 20,
            //    EvalSimulationMaxMoves = 20,
            //    EvalMaxMoves = 50,
            //};

            torch.set_num_threads(1);
            torch.set_num_interop_threads(1);

            var game = new Tixy(7, 7);
            var skynet = new TixySkynet(game, args);
            var evaluationSkynet = new TixySkynet(game, args);
            var greedyPlayer = new TixyGreedyPlayer(game);
            var coach = new Coach();
            coach.Run(game, skynet, evaluationSkynet, greedyPlayer, args);
        }

        static void Main(string[] _)
        {
            while (true)
            {
                try
                {
                    Run(0);
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