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
                SelfPlaySimulationIterations = 500,
            };

            const int W = 5;
            const int H = 5;

            var game = new Tixy(W, H);
            var skynet = new TixySkynet(game, new TixyParameters());

            string modelPath = $"c:\\temp\\zerosharp\\tixy {W}x{H}\\tixy-best.skynet";
            Console.WriteLine($"Loading model at {modelPath}...");

            skynet.LoadModel(modelPath);

            var mcts = new Mcts(game, skynet, args);

            var tixyPlayer = new MctsPlayer("", false, game, mcts);
            var greedy = new TixyGreedyPlayer(game);
            var humanPlayer = new TixyHumanPlayer(game);

            for (int i = 0; i < 100; i++)
            {
                var fight = new OneVsOne(game, humanPlayer, tixyPlayer);
                var res = fight.Run();
                Console.WriteLine($"Game over, result: {res}");
            }
            Console.ReadLine();
        }

        static void Main(string[] _)
        {
            Run();
        }
    }
}