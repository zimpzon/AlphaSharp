using AlphaSharp;
using TixyGame;
using TorchSharp;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run()
        {
            // DEEPMIND TWEAKS: https://lczero.org/blog/2018/12/alphazero-paper-and-lc0-v0191/

            // TRAINING ALGO: when game is won, track back a number of states (random around half of avg count?) and start from there, NOT picking the same action again.
            // Forwards and backwards meets at the middle'ish? Better endgame? I Assume endgame can be weak due to not always reaching it?

            // HMM, could we speed up a fresh start by generating a large number of random games without using the network?
            // Would be very, very fast and still hold lots of information.
            const int W = 5;
            const int H = 5;

            var alphaParam = new AlphaParameters
            {
                // global
                Iterations = 1000,
                ResumeFromCheckpoint = true,
                MaxLogLevel = LogLevel.MoreInfo,         // Show loss information during training
                MaxWorkerThreads = 1,
                MaxTrainingExamples = 100_000,
                OutputFolder = $"c:\\temp\\zerosharp\\Tixy {W}x{H}",

                TemperatureThresholdMoves = 15,          // Reduced from 30 - Tixy games ~30-50 moves
                DeduplicateTrainingData = true,
                DrawOptimalEvaluation = false,           // Standard win-based evaluation (Tixy is not draw-optimal)

                CpuctSelfPlay = 1.5f,                    // Reduced from 2.5 - better exploration/exploitation balance
                CpuctEvaluation = 1.5f,                  // Reduced from 2.5 - better evaluation

                // self-play
                SelfPlayEpisodes = 150,                  // Increased from 100 - more training data
                SelfPlaySimulationIterations = 200,     // Increased from 50 - critical for complex game
                SelfPlaySleepCycleChance = 0.0f,
                SelfPlaySleepNoiseChance = 0.0f,
                DirichletNoiseShape = 0.3f,              // Increased from 0.05 - more reasonable
                DirichletNoiseScale = 0.25f,             // Reduced from 100.0! - was causing too much randomness
                RandomOutOfNowherePct = 0.99f,

                // evaluation
                EvaluationRounds = 40,                   // Increased from 30 - more reliable evaluation
                EvalSimulationIterations = 150,         // Increased from 50 - better evaluation quality
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 15,                     // Increased from 10 - more training per iteration
                TrainingBatchSize = 64,                  // Increased from 32 - better gradient estimates
                TrainingBatchesPerEpoch = 1000,          // Reduced from 10,000 - was excessive, caused overfitting
                TrainingLearningRate = 0.003f,           // Increased from 0.001 - faster learning for complex game
                TrainingMaxWorkerThreads = 8,
            };

            // setting threads to 1 seems to be rather important for inference. more than 1 *always* slows down torch in my testing. Training can have a few.
            torch.set_num_threads(1);

            var game = new Tixy(W, H);

            var skynetCreator = () => new TixySkynet(game, tixyParam);

            var trainer = new AlphaSharpTrainer(game, skynetCreator, alphaParam);
            trainer.Run();
        }

        static void Main(string[] _)
        {
            while (true)
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
}