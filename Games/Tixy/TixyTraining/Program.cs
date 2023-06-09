using AlphaSharp;
using TixyGame;
using TorchSharp;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run()
        {
            // +progress callback
            // progress print helpers
            // tic tac from py
            // subfolders in project
            // beautiful progress, something. with stats for all iterations.

            var alphaParam = new AlphaParameters
            {
                // global
                ResumeFromCheckpoint = true,
                Iterations = 1000,
                MaxWorkerThreads = 1, // diminishing returns, 4 threads seems optimal'ish on home pc with 12/24 cores
                MaxTrainingExamples = 100000,
                Cpuct = 1.0f,
                OutputFolder = "c:\\temp\\zerosharp\\tixy",

                // self-play
                SelfPlaySimulationCount = 800,
                SelfPlaySimulationMaxMoves = 300,
                SelfPlayEpisodeMaxMoves = 150,
                SelfPlayEpisodes = 30,

                // evaluation
                EvaluationRounds = 10,
                EvaluationSimulationCount = 500,
                EvaluationSimulationMaxMoves = 200,
                EvaluationMaxMoves = 150,
            };

            var tixyParam = new TixyParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 64,
                TrainingLearningRate = 0.001f,
            };

            // setting threads to 1 seems to be rather important. more than 1 *always* slows down torch in my testing.
            torch.set_num_threads(1);

            var game = new Tixy(6, 6);
            var skynetCreator = () => new TixySkynet(game, tixyParam);
            var skynet = new TixySkynet(game, tixyParam);
            var evaluationSkynet = new TixySkynet(game, tixyParam);

            var trainer = new AlphaSharpTrainer(game, skynetCreator, alphaParam);
            trainer.Run();
        }

        static void Main(string[] _)
        {
            try
            {
                Run();
                Console.WriteLine("Exiting");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}