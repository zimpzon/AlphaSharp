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
                MaxLogLevel = LogLevel.Info,
                DirichletNoiseScale = 0,
            };

            var game = new TicTacToe();
            var skynet = new TicTacToeSkynet(game, new TicTacToeParameters());

            string modelPath = "c:\\temp\\zerosharp\\TicTacToe\\tic-tac-toe-best.skynet";
            Console.WriteLine($"Loading model at {modelPath}...");

            skynet.LoadModel(modelPath);

            var mcts = new Mcts(game, skynet, args);
            var ticTacToePlayer = new MctsPlayer("ai", false, game, mcts);
            var humanPlayer = new TicTacToeHumanPlayer(game);

            var fight = new OneVsOne(game, ticTacToePlayer, humanPlayer);
            var res = fight.Run();
            Console.WriteLine($"Game over, result: {res}\n");
            game.PrintState(fight.State, Console.WriteLine);
        }

        static void Main(string[] _)
        {
            while (true)
                Run();
        }
    }
}