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

        public int PickAction(byte[] state)
        {
            _game.PrintState(state, Console.WriteLine);
            Console.WriteLine();

            byte[] validActions = new byte[_game.ActionCount];
            _game.GetValidActions(state, validActions);

            var list = new List<string>();
            for (int i = 0; i < _game.ActionCount; i++)
            {
                if (validActions[i] != 0)
                    list.Add(i.ToString());
            }

            Console.WriteLine($"Pick a move: {string.Join(", ", list)}");

            while (true)
            {
                Console.WriteLine("Your move, human:");
                string inputMove = Console.ReadLine();
                if (!list.Contains(inputMove))
                {
                    Console.WriteLine("Invalid input, human");
                    continue;
                }

                int action = int.Parse(inputMove);
                return action;
            }
        }
    }
}
