using AlphaSharp.Interfaces;
using System.Text.RegularExpressions;

namespace TixyGame
{
    public partial class TixyHumanPlayer : IPlayer
    {
        private readonly IGame _game;
        private readonly Dictionary<string, int> colMapping = new ()
        {
            {"A", 0},
            {"B", 1},
            {"C", 2},
            {"D", 3},
            {"E", 4},
            {"F", 5},
            {"G", 6}
        };

        private List<Tuple<int, int, int, int, int, int>> moves;

        public TixyHumanPlayer(IGame game)
        {
            _game = game;
        }

        private Tuple<Tuple<int, int>, Tuple<int, int>> ParseInput(string inputMove)
        {
            inputMove = inputMove.ToUpper();

            var fromTo = inputMove.Split();

            var fromCol = colMapping[fromTo[0][0].ToString()];
            var fromRow = int.Parse(fromTo[0][1].ToString()) - 1;

            var toCol = colMapping[fromTo[1][0].ToString()];
            var toRow = int.Parse(fromTo[1][1].ToString()) - 1;

            return new Tuple<Tuple<int, int>, Tuple<int, int>>(
                new Tuple<int, int>(fromRow, fromCol),
                new Tuple<int, int>(toRow, toCol));
        }

        private int? FindMove(Tuple<int, int> fromPos, Tuple<int, int> toPos)
        {
            var dy = toPos.Item1 - fromPos.Item1;
            var dx = toPos.Item2 - fromPos.Item2;

            foreach (var move in moves)
            {
                if (move.Item2 == fromPos.Item1 && move.Item3 == fromPos.Item2 && move.Item4 == dx && move.Item5 == dy)
                {
                    return move.Item1;
                }
            }

            return null;
        }

        private static bool IsValidInput(string inputMove)
        {
            var pattern = MatchMoveRegex();
            var match = pattern.Match(inputMove);
            return match.Success;
        }

        public int PickAction(byte[] state)
        {
            _game.PrintState(state, Console.WriteLine);
            Console.WriteLine();

            moves = new List<Tuple<int, int, int, int, int, int>>();
            byte[] validActions = new byte[_game.ActionCount];
            _game.GetValidActions(state, validActions);

            for (int i = 0; i < _game.ActionCount; i++)
            {
                if (validActions[i] == 1)
                {
                    int action = i;
                    TixyPieces.DecodeAction(_game, state, action, out int row, out int col, out int piece, out int dx, out int dy);

                    moves.Add(new Tuple<int, int, int, int, int, int>(action, row, col, dx, dy, piece));
                }
            }

            while (true)
            {
                Console.WriteLine("Your move, punk:");
                string inputMove = Console.ReadLine();
                if (!IsValidInput(inputMove))
                {
                    Console.WriteLine("Invalid input, punk");
                    continue;
                }

                var parsedInput = ParseInput(inputMove);
                int? validMove = FindMove(parsedInput.Item1, parsedInput.Item2);
                if (validMove == null)
                {
                    Console.WriteLine("Invalid move, punk");
                    continue;
                }

                return validMove.Value;
            }
        }

        [GeneratedRegex("^[A-Ga-g][1-7]\\s[A-Ga-g][1-7]$")]
        private static partial Regex MatchMoveRegex();
    }
}
