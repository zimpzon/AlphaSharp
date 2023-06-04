﻿using AlphaSharp;
using TixyGame;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run(int id)
        {
            var args = new Args
            {
                ResumeFromCheckpoint = true,
                Iterations = 100,

                // self-play
                TrainingSimulationCount = 100,
                TrainingSimulationMaxMoves = 80,
                TrainingEpisodeMaxMoves = 80,
                TrainSelfPlayEpisodes = 10,
                
                // net training
                TrainingEpochs = 10,
                TrainingLearningRate = 0.001f,
                TrainingBatchSize = 64,
                TrainingMaxExamples = 100000,
                Cpuct = 1.0f,

                // evaluation
                EvalSimulationCount = 20,
                EvalSimulationMaxMoves = 50,
                EvalMaxMoves = 50,
            };

            //args = new Args
            //{
            //    // self-play
            //    TrainingSimulationCount = 10,
            //    TrainingSimulationMaxMoves = 10,
            //    TrainingEpisodeMaxMoves = 10,

            //    // net training
            //    TrainEpochs = 10,
            //    TrainLearningRate = 0.001f,
            //    TrainPlayEpisodes = 10,

            //    // evaluation
            //    EvalSimulationCount = 20,
            //    EvalSimulationMaxMoves = 20,
            //    EvalMaxMoves = 50,
            //};

            var game = new Tixy(5, 5);
            var skynet = new TixySkynet(game, args);

            var coach = new Coach();
            coach.Run(game, skynet, "c:\\temp\\zerosharp\\zerosharp", args);

            //int win1 = 0;
            //int win2 = 0;
            //int draw = 0;

            //// var player1 = new RandomPlayer(game);
            //var player1 = new MctsPlayer(game, skynet, new Args { PlayingSimulationCount = 10, PlayingSimulationMaxMoves = 50 });
            //var player2 = new MctsPlayer(game, skynet, new Args { PlayingSimulationCount = 10, PlayingSimulationMaxMoves = 50 });

            //for (int i = 0; i < 10; ++i)
            //{
            //    var pit = new OneVsOne(game, player1, player2);
            //    int gameResult = pit.Run(maxMoves: 50);
            //    if (gameResult == 1)
            //        ++win1;
            //    else if (gameResult == -1)
            //        ++win2;
            //    else
            //        ++draw;

            //    Console.WriteLine($"{id}: win1: {win1}, win2: {win2}, draw: {draw}");
            //}
        }

        static void Main(string[] _)
        {
            Run(1);
            Console.WriteLine("Done");
        }
    }
}