using AlphaSharp;
using TixyGame;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run()
        {
            var args = new AlphaParameters
            {
                // evaluation
                EvaluationSimulationCount = 500,
                EvaluationSimulationMaxMoves = 250,
                EvaluationMaxMoves = 150,
            };

            var game = new Tixy(5, 5);
            var skynet = new TixySkynet(game, new TixyParameters());

            string modelPath = "c:\\temp\\zerosharp\\tixy\\tixy-best.skynet";
            Console.WriteLine($"Loading model at {modelPath}...");

            skynet.LoadModel(modelPath);

            var tixyPlayer = new MctsPlayer(game, skynet, args);
            var humanPlayer = new TixyHumanPlayer(game);

            var fight = new OneVsOne(game, humanPlayer, tixyPlayer);
            var res = fight.Run(50);
            Console.WriteLine($"Game over, result: {res}");
        }

        static void Main(string[] _)
        {
            Run();
        }
    }
}