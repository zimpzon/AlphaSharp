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

            // WATCH some sample games!

            // WHY are selectedActions counts so low? was it just end of game?

            // reintroduce greedy!! (optional)

            // sim iterations start high then decay? since its same mcts it makes sense.

            // discard parts of tree no longer needed. should allow much deeper search.

            // FIX THE CRASHING BUG!!! MIGHT DISTURB RESULTS
            // ---- crashing is PROBABLY due to C# memory getting garbage collected while torch is still using it.

            // TRAINING ALGO: when game is won, track back a number of states (random around half of avg count?) and start from there, NOT picking the same action again.
            // Forwards and backwards meets at the middle'ish? Better endgame? I Assume endgame can be weak due to not always reaching it?

            const int W = 5;
            const int H = 6;

            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 4,
                MaxTrainingExamples = 50_000,
                OutputFolder = $"c:\\temp\\zerosharp\\Tixy {W}x{H}",
                TemperatureThresholdMoves = 20,
                SelfPlaySimulationIterations = 25,
                EvalSimulationIterations = 25,
                DirichletNoiseShape = 1.0f,
                DirichletNoiseScale = 1.0f,
                MaxLogLevel = LogLevel.Info,
                Cpuct = 1.5f, // AlphaZero uses ~10/game branching factor

                // self-play
                SelfPlayEpisodes = 100,
                SelfPlaySleepCycleChance = 0.2f,
                SelfPlaySleepNoiseChance = 0.25f,

                // evaluation
                EvaluationRounds = 100,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 32,
                TrainingLearningRate = 0.001f,
                TrainingMaxWorkerThreads = 8,
            };

            // setting threads to 1 seems to be rather important for inference. more than 1 *always* slows down torch in my testing. Training can have a few.
            torch.set_num_threads(1);

            var game = new Tixy(W, H);
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