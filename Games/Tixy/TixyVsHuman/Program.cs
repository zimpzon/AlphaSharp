﻿using AlphaSharp;
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
                SimulationIterations = 500,
            };

            var game = new Tixy(5, 5);
            var skynet = new TixySkynet(game, new TixyParameters());

            string modelPath = "c:\\temp\\zerosharp\\tixy\\tixy-best.skynet";
            Console.WriteLine($"Loading model at {modelPath}...");

            skynet.LoadModel(modelPath);

            var tixyPlayer = new MctsPlayer("", false, game, null);
            var humanPlayer = new TixyHumanPlayer(game);

            var fight = new OneVsOne(game, humanPlayer, tixyPlayer);
            var res = fight.Run();
            Console.WriteLine($"Game over, result: {res}");
        }

        static void Main(string[] _)
        {
            Run();
        }
    }
}