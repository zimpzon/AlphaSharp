using AlphaSharp.Interfaces;

namespace TicTacToeGame
{
    public class TicTacToeHumanPlayer : IPlayer
    {
        private readonly IGame _game;

        public TicTacToeHumanPlayer(IGame game)
        {
            _game = game;
        }

        public int PickAction(byte[] state, int _)
        {
            _game.PrintState(state, Console.WriteLine);
            Console.WriteLine();

            byte[] validActions = new byte[_game.ActionCount];
            _game.GetValidActions(state, validActions);

            var list = new List<string>();
            for (int i = 0; i < _game.ActionCount; i++)
            {
                if (validActions[i] != 0)
                    list.Add((i + 1).ToString());
            }

            Console.WriteLine($"Pick a move (1-9 for cell number): {string.Join(", ", list)}");

            while (true)
            {
                Console.WriteLine("Your move, human:");
                string inputMove = Console.ReadLine();
                if (!list.Contains(inputMove))
                {
                    Console.WriteLine("Invalid input, human");
                    continue;
                }

                int action = int.Parse(inputMove) - 1;
                return action;
            }
        }
    }
}
