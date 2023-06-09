using AlphaSharp;
using TicTacToeGame;
using TorchSharp;

namespace TicTacToeTraining
{
    internal static class Program
    {
        static void Run()
        {
            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 4, // diminishing returns, 4 threads seems optimal'ish on home pc with 12/24 cores
                MaxTrainingExamples = 100000,
                Cpuct = 1.0f,
                OutputFolder = "c:\\temp\\zerosharp\\TicTacToe",

                // self-play
                SelfPlaySimulationCount = 1000,
                SelfPlaySimulationMaxMoves = 200,
                SelfPlayEpisodeMaxMoves = 100,
                SelfPlayEpisodes = 40,

                // evaluation
                EvaluationRounds = 20,
                EvaluationSimulationCount = 1000,
                EvaluationSimulationMaxMoves = 200,
                EvaluationMaxMoves = 100,
            };

            var TicTacToeParam = new TicTacToeParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 64,
                TrainingLearningRate = 0.001f,
            };

            // setting threads to 1 seems to be rather important. more than 1 *always* slows down torch in my testing.
            torch.set_num_threads(1);

            var game = new TicTacToe();
            var skynetCreator = () => new TicTacToeSkynet(game, TicTacToeParam);
            var skynet = new TicTacToeSkynet(game, TicTacToeParam);
            var evaluationSkynet = new TicTacToeSkynet(game, TicTacToeParam);

            var trainer = new AlphaSharpTrainer(game, skynetCreator, alphaParam);
            trainer.Run();
        }

        static void Main(string[] _)
        {
            try
            {
                Run();
                Console.WriteLine("Exiting");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
