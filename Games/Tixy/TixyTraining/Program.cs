using AlphaSharp;
using TixyGame;
using TorchSharp;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run()
        {
            // getGameEnded cannot handle a draw. that requires 4 states, p1, p2, draw, still going.
            // a draw in tictac can be determined by state alone, but not so in Tixy.
            // also, no valid moves is a draw in tictac, but could be loss or valid in other games.

            // actually... doesn't it make a huge difference for the value of a state if next player i pl1 or pl2?
            // NO? because it is always players1 turn?
            //  should probably be encoded in it's own plane just like AlphaZero does!

            // tictac evaluation looks very weird. most of the time exactly 10-10, often 0-10, and then some 0-x-y with draws. mixed wins are rare or possibly non-existent.

            // training still crashes some times, very often with tictac

            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 4, // diminishing returns, 4 threads seems optimal'ish on home pc with 12/24 cores
                MaxTrainingExamples = 100000,
                OutputFolder = "c:\\temp\\zerosharp\\Tixy",
                TemperatureThresholdMoves = 20,
                SimulationIterations = 1000,
                SimulationMaxMoves = 300,
                DirichletNoiseAmount = 0.25f,
                DirichletNoiseShape = 0.3f,

                // self-play
                SelfPlayEpisodeMaxMoves = 150,
                SelfPlayEpisodes = 30,

                // evaluation
                EvaluationRounds = 10,
                EvaluationMaxMoves = 150,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 200,
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