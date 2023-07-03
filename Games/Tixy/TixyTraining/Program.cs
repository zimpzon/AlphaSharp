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

            // reintroduce greedy!! (optional)

            // discard parts of tree no longer needed. should allow much deeper search.

            // TRAINING ALGO: when game is won, track back a number of states (random around half of avg count?) and start from there, NOT picking the same action again.
            // Forwards and backwards meets at the middle'ish? Better endgame? I Assume endgame can be weak due to not always reaching it?

            // sleep cycle: decay randomness?

            // batch inference: fixed batch buffers, no matter if filled or not.

            const int W = 5;
            const int H = 5;

            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 8,
                MaxTrainingExamples = 50_000,
                OutputFolder = $"c:\\temp\\zerosharp\\Tixy {W}x{H}",
                TemperatureThresholdMoves = 20,
                SelfPlaySimulationIterations = 500,
                EvalSimulationIterations = 100,
                DirichletNoiseShape = 1.0f,
                DirichletNoiseScale = 1.0f,
                MaxLogLevel = LogLevel.Info,
                Cpuct = 1.5f, // AlphaZero uses ~10/game branching factor

                // self-play
                SelfPlayEpisodes = 200,
                SelfPlaySleepCycleChance = 0.2f,
                SelfPlaySleepNoiseChance = 0.25f,

                // evaluation
                EvaluationRounds = 50,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 15,
                TrainingBatchSize = 32,
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