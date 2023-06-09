﻿using AlphaSharp;
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
                EvaluationSimulationCount = 200,
                EvaluationSimulationMaxMoves = 150,
                EvaluationMaxMoves = 150,
            };

            var ticTacToeParam = new TicTacToeParameters
            {
                TrainingEpochs = 10,
                TrainingBatchSize = 64,
                TrainingLearningRate = 0.001f,
            };

            var game = new TicTacToe();
            var skynet = new TicTacToeSkynet(game, ticTacToeParam);

            string modelPath = "c:\\temp\\zerosharp\\TicTacToe\\best.skynet";
            Console.WriteLine($"Loading model at {modelPath}...");

            skynet.LoadModel(modelPath);

            var ticTacToePlayer = new MctsPlayer(game, skynet, args);
            var humanPlayer = new TicTacToeHumanPlayer(game);

            var fight = new OneVsOne(game, humanPlayer, ticTacToePlayer);
            var res = fight.Run(50);
            Console.WriteLine($"Game over, result: {res}\n");
        }

        static void Main(string[] _)
        {
            while (true)
                Run();
        }
    }
}