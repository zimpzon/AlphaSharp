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
                MaxLogLevel = LogLevel.Info,
                MaxWorkerThreads = 4,
                MaxTrainingExamples = 100_000,
                OutputFolder = $"c:\\temp\\zerosharp\\Tixy {W}x{H}",

                TemperatureThresholdMoves = 30,
                DeduplicateTrainingData = true,

                CpuctSelfPlay = 2.5f,
                CpuctEvaluation = 2.5f,

                // self-play
                SelfPlayEpisodes = 100,
                SelfPlaySimulationIterations = 50,
                SelfPlaySleepCycleChance = 0.25f,
                SelfPlaySleepNoiseChance = 0.05f,
                DirichletNoiseShape = 0.05f,
                DirichletNoiseScale = 100.0f, // noise values are very small and we want more or less total random
                RandomOutOfNowherePct = 0.99f,

                // evaluation
                EvaluationRounds = 30,
                EvalSimulationIterations = 50,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 32,
                TrainingBatchesPerEpoch = 10_000,
                TrainingLearningRate = 0.001f,
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