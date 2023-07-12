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

            const int W = 5;
            const int H = 5;

            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 6,
                MaxTrainingExamples = 50_000,
                OutputFolder = $"c:\\temp\\zerosharp\\Tixy {W}x{H}",
                TemperatureThresholdMoves = 100,
                SelfPlaySimulationIterations = 100,
                EvalSimulationIterations = 50,
                DirichletNoiseShape = 0.05f,
                DirichletNoiseScale = 1.0f,
                MaxLogLevel = LogLevel.Info,
                Cpuct = 4.0f, // AlphaZero uses ~10/game branching factor

                // self-play
                SelfPlayEpisodes = 30,
                SelfPlaySleepCycleChance = 0.25f,
                SelfPlaySleepNoiseChance = 0.15f,

                // evaluation
                EvaluationRounds = 30,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 64,
                TrainingBatchesPerEpoch = 1000,
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