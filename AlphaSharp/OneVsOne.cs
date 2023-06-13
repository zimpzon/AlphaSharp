using AlphaSharp.Interfaces;
using System;

namespace AlphaSharp
{
    public class OneVsOne
    {
        private readonly IGame _game;
        private readonly IPlayer _player1;
        private readonly IPlayer _player2;

        public OneVsOne(IGame game, IPlayer player1, IPlayer player2)
        {
            _game = game;
            _player1 = player1;
            _player2 = player2;
        }

        public GameOver.Status Run()
        {
            var state = new byte[_game.StateSize];
            var actions = new byte[_game.ActionCount];

            _game.GetStartingState(state);

            var currentPlayer = _player1;

            int moves = 0;
            var prevState = new byte[_game.StateSize];

            while (true)
            {
                _game.GetValidActions(state, actions);
                int validActionCount = ArrayUtil.CountNonZero(actions);
                if (validActionCount == 0)
                {
                    // no valid actions is considered a draw
                    return 0;
                }

                int selectedAction = currentPlayer.PickAction(state);
                _game.ExecutePlayerAction(state, selectedAction);
                //_game.PrintDisplayTextForAction(selectedAction, Console.WriteLine);
                //_game.PrintState(state, Console.WriteLine);
                moves++;

                var gameResult = _game.GetGameEnded(state, moves, isSimulation: false);
                if (gameResult != GameOver.Status.GameIsNotOver)
                {
                    // moves are always done by player1 so invert result if current player is actually player2
                    if (currentPlayer == _player2)
                        gameResult = GameOver.InvertResult(gameResult);

                    return gameResult;
                }

                _game.FlipStateToNextPlayer(state);
                Array.Copy(state, prevState, state.Length);

                currentPlayer = currentPlayer == _player1 ? _player2 : _player1;
            }
        }
    }
}
