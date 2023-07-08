using AlphaSharp;
using TixyGame;

namespace FakeGame
{
    internal static class Program
    {
        static void Main(string[] _)
        {
            var args = new AlphaParameters
            {
                DirichletNoiseScale = 0,
                Iterations = 10000,
                SelfPlayEpisodes = 10,
                SelfPlaySimulationIterations = 25,
                TemperatureThresholdMoves = 10000,
                OutputFolder = "c:\\temp\\zerosharp\\Fakegame",
                MaxLogLevel = LogLevel.Debug,
                MaxWorkerThreads = 1,
            };

            var game = new FakeGame();

            var param = new GenericSkynetParam
            {
                NumberOfPieces = 2,
                TrainingMaxWorkerThreads = 1
            };
            var pieceToLayer = new Dictionary<byte, int>

            {
                [1] = 0,
                [2] = 1,
                [255] = 2
            };


            var skynetCreator = () => new GenericSkynet(game, param, pieceToLayer);

            var trainer = new AlphaSharpTrainer(game, () => new FakeGameSkynet(game), args);
            try
            {
                trainer.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}