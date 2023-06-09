using AlphaSharp.Interfaces;
using System;

namespace AlphaSharp
{
    public class OneVsOne
    {
        private readonly IGame _game;
        private readonly IPlayer _player1;
        private readonly IPlayer _player2;
        private bool _abort;

        public OneVsOne(IGame game, IPlayer player1, IPlayer player2)
        {
            _game = game;
            _player1 = player1;
            _player2 = player2;
        }

        public void Abort()
            => _abort = true;

        public int Run(int maxMoves)
        {
            var state = new byte[_game.StateSize];
            var actions = new byte[_game.ActionCount];

            _game.GetStartingState(state);

            var currentPlayer = _player1;

            int moves = 0;
            var prevState = new byte[_game.StateSize];

            while (true)
            {
                if (_abort)
                    return int.MinValue;

                if (moves++ >= maxMoves)
                {
                    // reaching maxMoves is considered a draw
                    return 0;
                }

                _game.GetValidActions(state, actions);
                int validActionCount = ArrayUtil.CountNonZero(actions);
                if (validActionCount == 0)
                {
                    // no valid actions is considered a draw
                    return 0;
                }

                int selectedAction = currentPlayer.PickAction(state);

                _game.ExecutePlayerAction(state, selectedAction);

                int gameResult = _game.GetGameEnded(state);
                if (gameResult != 0)
                    return currentPlayer == _player1 ? 1 : -1;

                _game.FlipStateToNextPlayer(state);
                Array.Copy(state, prevState, state.Length);

                currentPlayer = currentPlayer == _player1 ? _player2 : _player1;
            }
        }
    }
}
