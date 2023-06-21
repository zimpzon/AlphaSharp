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

            // FIX THE CRASHING BUG!!! MIGHT DISTURB RESULTS
            // ---- craching is PROBABLY due to C# memory getting garbage collected while torch is still using it.

            // DEDUPLICATE samples? makes sense that a large number of early game samples can skew training. dict[state, value] = list for unique? (just use state and average values!)

            // SHOULDN'T Tictactoe be able to conclude most games? I bet if watching the games they are just stupid random.

            // TRAINING ALGO: when game is won, track back a number of states (random around half of avg count?) and start from there, NOT picking the same action again.
            // Forwards and backwards meets at the middle'ish? Better endgame? I Assume endgame can be weak due to not always reaching it?

            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 1,
                MaxTrainingExamples = 20_000,
                OutputFolder = "c:\\temp\\zerosharp\\Tixy",
                TemperatureThresholdMoves = 30,
                SimulationIterations = 50,
                DirichletNoiseShape = 1.0f,
                MaxLogLevel = LogLevel.Info,
                Cpuct = 1.0f, // exploration term

                // self-play
                SelfPlayEpisodes = 10,

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

            var game = new Tixy(6, 6);
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