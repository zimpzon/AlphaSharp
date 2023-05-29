﻿using System.Diagnostics;
using AlphaSharp.Interfaces;

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

        public int Run(int maxMoves, byte[] state, byte[] actions)
        {
            _game.SetStartingState(state);

            var currentPlayer = _player1;

            int moves = 0;
            while (true)
            {
                if (moves++ >= maxMoves)
                    return 0;

                _game.GetValidActions(state, actions);
                int selectedAction = currentPlayer.PickAction(state, actions);

                //_game.PrintState(state, (s) => Debug.WriteLine(s));
                //_game.PrintDisplayTextForAction(selectedAction, (s) => Debug.WriteLine(s));

                _game.ExecutePlayerAction(state, selectedAction);
                int gameResult = _game.GetGameEnded(state);
                if (gameResult != 0)
                    return currentPlayer == _player1 ? 1 : -1;

                _game.FlipStateToNextPlayer(state);
                currentPlayer = currentPlayer == _player1 ? _player2 : _player1;
            }
        }
    }
}
