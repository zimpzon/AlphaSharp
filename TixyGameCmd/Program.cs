using AlphaSharp;
using TixyGame;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Main(string[] _)
        {
            var game = new Tixy(5, 7);
            int win1 = 0;
            int win2 = 0;
            int draw = 0;

            var player1 = new RandomPlayer(game);
            var player2 = new RandomPlayer(game);

            for (int i = 0; i < 10; ++i)
            {
                var pit = new OneVsOne(game, player1, player2);
                int gameResult = pit.Run(maxMoves: 50);
                if (gameResult == 1)
                    ++win1;
                else if (gameResult == -1)
                    ++win2;
                else
                    ++draw;

                Console.WriteLine($"win1: {win1}, win2: {win2}, draw: {draw}");
            }
        }
    }
}