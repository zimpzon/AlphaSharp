using AlphaSharp;
using TicTacToeGame;
using TixyGame;

namespace TicTacToeVsHuman
{
    internal class Program
    {
        static void Run()
        {
            var args = new AlphaParameters
            {
                // evaluation
                SelfPlaySimulationIterations = 200,
                MaxLogLevel = LogLevel.Info,
                DirichletNoiseScale = 0,
                EvalSimulationIterations = 5,
            };

            var game = new TicTacToe();

            var param = new GenericSkynetParam
            {
                NumberOfPieces = 2,
                TrainingMaxWorkerThreads = 1
            };

            var pieceToLayer = new Dictionary<byte, int>
            {
                [1] = 0,
                [2] = 1,
            };

            var skynet = new GenericSkynet(game, param, pieceToLayer);

            string modelPath = "c:\\temp\\zerosharp\\TicTacToe\\tic-tac-toe-best.skynet";
            Console.WriteLine($"Loading model at {modelPath}...");

            skynet.LoadModel(modelPath);

            var mcts = new Mcts(game, skynet, args);
            var ticTacToePlayer = new MctsPlayer("ai", false, game, mcts);
            var humanPlayer = new TicTacToeHumanPlayer(game);

            var fight = new OneVsOne(game, humanPlayer, ticTacToePlayer, verbose: true);
            var res = fight.Run();
            Console.WriteLine($"\n----- Game over, result: {res} ------\n");
        }

        static void Main(string[] _)
        {
            while (true)
                Run();
        }
    }
}