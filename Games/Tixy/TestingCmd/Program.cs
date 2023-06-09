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
                Iterations = 1000,
                MaxTrainingExamples = 100000,
                Cpuct = 1.0f,

                // self-play
                SelfPlaySimulationCount = 500,
                SelfPlaySimulationMaxMoves = 200,
                SelfPlayEpisodeMaxMoves = 80,
                SelfPlayEpisodes = 20,


                // evaluation
                EvaluationSimulationCount = 200,
                EvaluationSimulationMaxMoves = 150,
                EvaluationMaxMoves = 150,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 64,
                TrainingLearningRate = 0.001f,
            };

            var game = new Tixy(5, 5);
            var skynet = new TixySkynet(game, tixyParam);
            skynet.LoadModel("c:\\temp\\zerosharp\\best.skynet");

            var p1 = new RandomPlayer(game);
            var p2 = new TixyGreedyPlayer(game);
            var p3 = new MctsPlayer(game, skynet, args);
            var p4 = new TixyHumanPlayer(game);

            int won1 = 0;
            int won2 = 0;
            for (int i = 0; i < 100; ++i)
            {
                var fight = new OneVsOne(game, p4, p3);
                var res = fight.Run(50, verbose: true);
                won1 += res == 1 ? 1 : 0;
                won2 += res == 1 ? 0 : 1;
            }
            Console.WriteLine($"Won1: {won1}, Won2: {won2}");
        }

        static void Main(string[] _)
        {
            Run();
            Console.WriteLine("Done");
        }
    }
}