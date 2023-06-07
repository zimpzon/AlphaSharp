using AlphaSharp;
using TixyGame;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run()
        {
            var args = new Args
            {
                ResumeFromCheckpoint = true,
                ResumeFromEval = false,
                Iterations = 1000,

                // self-play
                SelfPlaySimulationCount = 500,
                SelfPlaySimulationMaxMoves = 200,
                SelfPlayEpisodeMaxMoves = 80,
                selfPlayEpisodes = 20,

                // net training
                TrainingEpochs = 10,
                TrainingLearningRate = 0.001f,
                TrainingBatchSize = 64,
                SelfPlayMaxExamples = 100000,
                Cpuct = 1.0f,

                // evaluation
                EvalSimulationCount = 200,
                EvalSimulationMaxMoves = 150,
                EvalMaxMoves = 150,
            };

            var game = new Tixy(5, 7);
            var skynet = new TixySkynet(game, args);
            skynet.LoadModel("c:\\temp\\zerosharp\\tixy-model-best.pt");
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