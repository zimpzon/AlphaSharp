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
                Iterations = 5,
                SelfPlayEpisodes = 10,
                SimulationIterations = 4,
                TemperatureThresholdMoves = 10000,
                OutputFolder = "c:\\temp\\zerosharp\\Fakegame",
                MaxLogLevel = LogLevel.Info,
            };

            var game = new FakeGame();

            var trainer = new AlphaSharpTrainer(game, () => new FakeGameSkynet(game), args);
            trainer.Run();
        }
    }
}