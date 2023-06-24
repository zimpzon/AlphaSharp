using AlphaSharp;
using TixyGame;
using TorchSharp;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run()
        {
            // reintroduce greedy!! (optional)

            // MULTITHREADED: buffers are per thread, virtual loss somehow, discourage path, but don't block it. I assume.

            // FIX THE CRASHING BUG!!! MIGHT DISTURB RESULTS
            // ---- crashing is PROBABLY due to C# memory getting garbage collected while torch is still using it.

            // TRAINING ALGO: when game is won, track back a number of states (random around half of avg count?) and start from there, NOT picking the same action again.
            // Forwards and backwards meets at the middle'ish? Better endgame? I Assume endgame can be weak due to not always reaching it?

            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 4,
                MaxTrainingExamples = 50_000,
                OutputFolder = "c:\\temp\\zerosharp\\Tixy",
                TemperatureThresholdMoves = 40,
                SelfPlaySimulationIterations = 100,
                EvalSimulationIterations = 50,
                DirichletNoiseShape = 1.0f,
                DirichletNoiseScale = 10.0f,
                MaxLogLevel = LogLevel.Info,
                Cpuct = 1.0f,

                // self-play
                SelfPlayEpisodes = 20,

                // evaluation
                EvaluationRounds = 30,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 32,
                TrainingLearningRate = 0.001f,
            };

            // setting threads to 1 seems to be rather important. more than 1 *always* slows down torch in my testing.
            torch.set_num_threads(1);

            var game = new Tixy(5, 5);
            alphaParam.ExtraComparePlayer = new TixyGreedyPlayer(game);

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