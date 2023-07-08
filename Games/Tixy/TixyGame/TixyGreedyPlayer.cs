using AlphaSharp.Interfaces;

namespace TixyGame
{
    public class TixyGreedyPlayer : IPlayer
    {
        public string Name => "TixyGreedy";

        private readonly IGame _game;

        public TixyGreedyPlayer(IGame game)
        {
            _game = game;
        }

        public int PickAction(byte[] state, int _)
        {
            var validActions = new byte[_game.ActionCount];
            _game.GetValidActions(state, validActions);

            for (int i = 0; i < _game.ActionCount; i++)
            {
                if (validActions[i] == 0)
                    continue;

                int action = i;
                TixyPieces.DecodeAction(_game, state, action, out int row, out int _, out int piece, out int _, out int dy);

                bool isWinningMove = row + dy == 0 && piece == TixyPieces.P1.I;
                if (isWinningMove)
                    return action;

                bool canCapture = piece != 0;
                if (canCapture)
                    return action;
            }

            int randomAction;
            do
            {
                randomAction = new Random().Next(_game.ActionCount);
            }
            while (validActions[randomAction] != 1);

            return randomAction;
        }
    }
}