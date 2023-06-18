using AlphaSharp;

namespace FakeGame
{
    internal static class Program
    {
        static void Main(string[] _)
        {
            var args = new AlphaParameters
            {
                DirichletNoiseAmount = 0,
                Iterations = 10000,
                SelfPlayEpisodes = 10,
                SimulationIterations = 25,
                TemperatureThresholdMoves = 10000,
                OutputFolder = "c:\\temp\\zerosharp\\Fakegame",
                MaxLogLevel = LogLevel.Debug,
                MaxWorkerThreads = 1,
            };

            var game = new FakeGame();

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