using AlphaSharp.Interfaces;
using System;

namespace AlphaSharp
{
    public class MctsPlayer : IPlayer
    {
        public string Name { get; }

        private readonly Mcts _mcts;
        private readonly IGame _game;
        private int _moveCount;
        private readonly bool _firstMoveIsRandom;

        public MctsPlayer(string name, bool firstMoveIsRandom, IGame game, Mcts mcts)
        {
            Name = name;
            _game = game;
            _firstMoveIsRandom = firstMoveIsRandom;
            _mcts = mcts;
        }

        public int PickAction(byte[] state, int playerTurn)
        {
            _moveCount++;

            if (_moveCount == 1 && _firstMoveIsRandom)
            {
                var validActions = new byte[_game.ActionCount];
                _game.GetValidActions(state, validActions);
                int validActionCount = Util.CountNonZero(validActions);
                int selected = new Random().Next(validActionCount);
                int action = Util.FindNthNonZeroIndex(validActions, selected + 1);
                return action;
            }

            var probs = _mcts.GetActionPolicy(state, playerTurn, simulationDecay: 1.0f);
            return Util.ArgMax(probs);
        }
    }
}
;