using AlphaSharp.Interfaces;
using System;

namespace AlphaSharp
{
    public class OneVsOne
    {
        private readonly IGame _game;
        private readonly IPlayer _player1;
        private readonly IPlayer _player2;
        public byte[] State { get; private set; }

        public OneVsOne(IGame game, IPlayer player1, IPlayer player2)
        {
            _game = game;
            _player1 = player1;
            _player2 = player2;
        }

        public GameOver.Status Run()
        {
            State = new byte[_game.StateSize];
            var actions = new byte[_game.ActionCount];

            _game.GetStartingState(State);

            var currentPlayer = _player1;
            int playerTurn = 1;
            int moves = 0;
            var prevState = new byte[_game.StateSize];

            while (true)
            {
                _game.GetValidActions(State, actions);
                int validActionCount = ArrayUtil.CountNonZero(actions);
                if (validActionCount == 0)
                {
                    // no valid actions is considered a draw
                    return 0;
                }

                int selectedAction = currentPlayer.PickAction(State, playerTurn);

                //Console.WriteLine($"before move ({playerTurn}) :");
                //if (playerTurn == -1)
                //    _game.FlipStateToNextPlayer(state);
                //_game.PrintState(state, Console.WriteLine);
                //if (playerTurn == -1)
                //    _game.FlipStateToNextPlayer(state);
                //_game.PrintDisplayTextForAction(selectedAction, Console.WriteLine);

                _game.ExecutePlayerAction(State, selectedAction);

                //if (playerTurn == -1)
                //    _game.FlipStateToNextPlayer(state);

                //Console.WriteLine($"after move ({playerTurn}) :");
                //_game.PrintState(state, Console.WriteLine);
                //if (playerTurn == -1)
                //    _game.FlipStateToNextPlayer(state);

                moves++;

                var gameResult = _game.GetGameEnded(State, moves, isSimulation: false);
                if (gameResult != GameOver.Status.GameIsNotOver)
                {
                    // moves are always done by player1 so invert result if current player is actually player2
                    if (currentPlayer == _player2)
                        gameResult = GameOver.InvertResult(gameResult);

                    return gameResult;
                }

                _game.FlipStateToNextPlayer(State);
                Array.Copy(State, prevState, State.Length);

                playerTurn = -playerTurn;
                currentPlayer = currentPlayer == _player1 ? _player2 : _player1;
            }
        }
    }
}
