using AlphaSharp;
using AlphaSharp.Interfaces;
using TixyGame;

namespace TixyGameCmd
{
    internal static class Program
    {
        static IGame game = new Tixy(5, 4);
        static ISkynet skynet = new TixySkynet(game);

        static void Run(int id)
        {
            var coach = new Coach();
            coach.Run(game, skynet, "c:\\temp\\zerosharp", new Args {
                TrainingSimulationCount = 10,
                TrainingSimulationMaxMoves = 10,
                TrainingEpisodeMaxMoves = 10
            });

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