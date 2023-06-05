using AlphaSharp;
using TixyGame;

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
                SelfPlaySimulationCount = 100,
                SelfPlaySimulationMaxMoves = 50,
                SelfPlayEpisodeMaxMoves = 50,
                selfPlayEpisodes = 10,
                
                // net training
                TrainingEpochs = 10,
                TrainingLearningRate = 0.001f,
                TrainingBatchSize = 64,
                SelfPlayMaxExamples = 100000,
                Cpuct = 1.0f,

                // evaluation
                EvalSimulationCount = 100,
                EvalSimulationMaxMoves = 50,
                EvalMaxMoves = 50,
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

            //torch.set_num_threads(1);
            //torch.set_num_interop_threads(2);

            var game = new Tixy(5, 5);
            var skynet = new TixySkynet(game, args);
            var evaluationSkynet = new TixySkynet(game, args);

            var coach = new Coach();
            coach.Run(game, skynet, evaluationSkynet, args);
        }

        static void Main(string[] _)
        {
            Run(1);
            Console.WriteLine("Done");
        }
    }
}