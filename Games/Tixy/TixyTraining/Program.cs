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
            // !!! look at python 'game ended at step 14' and print some more mcts debug!!! should be comparable.

            // tictac evaluation looks very weird. most of the time exactly 10-10, often 0-10, and then some 0-x-y with draws. mixed wins are rare or possibly non-existent.

            // training still crashes some times, very often with tictac

            // TRAINING ALGO: when game is won, track back a number of states (random around half of avg count?) and start from there, NOT picking the same action again.
            // Forwards and backwards meets at the middle'ish? Better endgame? I Assume endgame can be weak due to not always reaching it?

            // ACTION CONSIDER: print state and considered moves. for debug.
            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 1, // diminishing returns, 4 threads seems optimal'ish on home pc with 12/24 cores
                MaxTrainingExamples = 100000,
                OutputFolder = "c:\\temp\\zerosharp\\Tixy",
                TemperatureThresholdMoves = 10000,
                SimulationIterations = 300,
                DirichletNoiseAmount = 0.5f,
                DirichletNoiseShape = 0.03f,
                EvaluationPlayers = EvaluationPlayers.AlternatingModels,
                MaxLogLevel = LogLevel.Debug,

                // self-play
                SelfPlayEpisodes = 40,

                // evaluation
                EvaluationRounds = 10,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 64,
                TrainingLearningRate = 0.001f,
            };

            // setting threads to 1 seems to be rather important. more than 1 *always* slows down torch in my testing.
            torch.set_num_threads(1);

            var game = new Tixy(5, 5);
            var skynetCreator = () => new TixySkynet(game, tixyParam);

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