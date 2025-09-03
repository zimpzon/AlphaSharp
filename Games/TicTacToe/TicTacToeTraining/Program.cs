using AlphaSharp;
using TicTacToeGame;
using TixyGame;
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
                ResumeFromCheckpoint = false,
                Iterations = 1000,
                MaxWorkerThreads = 1, // diminishing returns, 4 threads seems optimal'ish on home pc with 12/24 cores
                OutputFolder = "c:\\temp\\zerosharp\\TicTacToe",
                MaxLogLevel = LogLevel.MoreInfo,     // Show loss information during training
                SelfPlaySimulationIterations = 400,  // Increased from 10 - critical for learning
                TemperatureThresholdMoves = 4,       // Reduced from 10 - TicTacToe games are short (5-9 moves)
                EvalSimulationIterations = 200,      // Increased from 5 - better evaluation
                SelfPlayEpisodes = 300,              // Increased from 50 - more training data
                EvaluationRounds = 30,
                DrawOptimalEvaluation = true,       // Use draw-optimal evaluation for TicTacToe (draws are optimal play)
                SelfPlaySleepCycleChance = 0.5f,
                SelfPlaySleepNoiseChance = 0.1f,
                CpuctSelfPlay = 1.0f,                // Reduced from 4.0 - less exploration, more exploitation
                DirichletNoiseScale = 0.3f,          // Reduced from 10.0 - less noise for simple game
                DirichletNoiseShape = 1.0f,
            };

            // setting threads to 1 seems to be rather important. more than 1 *always* slows down torch in my testing.
            torch.set_num_threads(1);

            var game = new TicTacToe();

            var param = new GenericSkynetParam
            {
                NumberOfPieces = 2,
                TrainingMaxWorkerThreads = 4,
                TrainingEpochs = 20,              // Increased from 10 - more training per iteration
                TrainingBatchSize = 64,          // Increased from default 32 - better gradient estimates
                TrainingLearningRate = 0.01f,    // Increased from 0.001 - faster learning for simple game
            };

            var skynetCreator = () => new TicTacToeSkynet(game, param);
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
