using AlphaSharp;
using AlphaSharp.Interfaces;
using System.Diagnostics;
using System.Threading.Tasks;
using TixyGame;

namespace TixyGameCmd
{
    internal static class Program
    {
        static IGame game= new Tixy(5, 7);
        static ISkynet skynet = new TixySkynet(game);

        static void Run(int id)
        {
            
            var proc = Process.GetCurrentProcess();
            var dddd = Process.GetCurrentProcess().ProcessorAffinity;
            int win1 = 0;
            int win2 = 0;
            int draw = 0;

            var player1 = new RandomPlayer(game);
            var player2 = new MctsPlayer(game, skynet, new Args { SimCountPlay = 10, SimMaxMoves = 50 });

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
            var t1 = new Thread(() => Run(1));
            //var t2 = new Thread(() => Run(1));
            //var t3 = new Thread(() => Run(1));
            //var t4 = new Thread(() => Run(1));

            t1.Start();
            //t2.Start();
            //t3.Start();
            //t4.Start();

            t1.Join();
            //t2.Join();
            //t3.Join();
            //t4.Join();

            Console.WriteLine("Done");
        }
    }
}