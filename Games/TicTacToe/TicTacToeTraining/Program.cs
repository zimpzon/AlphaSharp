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
                SelfPlaySimulationIterations = 10,
                TemperatureThresholdMoves = 10,
                EvalSimulationIterations = 5,
                SelfPlayEpisodes = 50,
                EvaluationRounds = 30,
                SelfPlaySleepCycleChance = 0.5f,
                SelfPlaySleepNoiseChance = 0.1f,
                CpuctSelfPlay = 4.0f,
                DirichletNoiseScale = 10.0f,
                DirichletNoiseShape = 1.0f,
            };

            // setting threads to 1 seems to be rather important. more than 1 *always* slows down torch in my testing.
            torch.set_num_threads(1);

            var game = new TicTacToe();

            var param = new GenericSkynetParam
            {
                NumberOfPieces = 2,
                TrainingMaxWorkerThreads = 4,
                TrainingEpochs = 10,
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
