using AlphaSharp;
using System.Threading.Tasks;
using TixyGame;

namespace TixyGameCmd
{
    internal static class Program
    {
        static void Run(int id)
        {
            var game = new Tixy(5, 7);
            int win1 = 0;
            int win2 = 0;
            int draw = 0;

            var player1 = new RandomPlayer(game);
            var player2 = new MctsPlayer(game, new TixySkynet(game), new Args { SimCountPlay = 10, SimMaxMoves = 50 });

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

                Console.WriteLine($"{id}: win1: {win1}, win2: {win2}, draw: {draw}");
            }
        }

        static void Main(string[] _)
        {
            var task1 = Task.Run(() => Run(1));
            var task2 = Task.Run(() => Run(2));
            //var task3 = Task.Run(() => Run(3));
            //var task4 = Task.Run(() => Run(4));
            Task.WaitAll(task1, task2);
            //Task.WaitAll(task1, task2, task3, task4);
        }
    }
}