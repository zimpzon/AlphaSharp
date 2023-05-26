using AlphaSharp;
using TixyGame;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Main(string[] _)
        {
            // training loop:
            // get two nn players with same nn version
            // play against self * x, output training data
            // train nn
            // play against previus (+ random, greedy)
            // reject or accept new nn
            // repeat

            var player1 = new RandomPlayer();
            var player2 = new RandomPlayer();

            var game = new Tixy(5, 7);
            int win1 = 0;
            int win2 = 0;
            int draw = 0;
            for (int i = 0; i < 1000; ++i)
            {
                var pit = new OneVsOne(game, player1, player2);
                int gameResult = pit.Run(50);
                if (gameResult == 1)
                    ++win1;
                else if (gameResult == -1)
                    ++win2;
                else
                    ++draw;
            }

            Console.WriteLine($"win1: {win1}, win2: {win2}, draw: {draw}");
        }
    }
}