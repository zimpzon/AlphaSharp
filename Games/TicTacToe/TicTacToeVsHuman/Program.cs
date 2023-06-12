using AlphaSharp;
using TicTacToeGame;

namespace TicTacToeVsHuman
{
    internal class Program
    {
        static void Run()
        {
            var args = new AlphaParameters
            {
                // evaluation
                SimulationIterations = 200,
            };

            var game = new TicTacToe();
            var skynet = new TicTacToeSkynet(game, new TicTacToeParameters());

            string modelPath = "c:\\temp\\zerosharp\\TicTacToe\\tic-tac-toe-best.skynet";
            Console.WriteLine($"Loading model at {modelPath}...");

            skynet.LoadModel(modelPath);

            var ticTacToePlayer = new MctsPlayer(game, skynet, args);
            var humanPlayer = new TicTacToeHumanPlayer(game);

            var fight = new OneVsOne(game, humanPlayer, ticTacToePlayer);
            var res = fight.Run();
            Console.WriteLine($"Game over, result: {res}\n");
        }

        static void Main(string[] _)
        {
            while (true)
                Run();
        }
    }
}