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
        private readonly byte[] _validActions;

        public MctsPlayer(string name, bool firstMoveIsRandom, IGame game, Mcts mcts)
        {
            Name = name;
            _game = game;
            _firstMoveIsRandom = firstMoveIsRandom;
            _mcts = mcts;
            _validActions = new byte[_game.ActionCount];
        }

        public int PickAction(byte[] state, int playerTurn)
        {
            _moveCount++;

            _game.GetValidActions(state, _validActions);

            if (_moveCount == 1 && _firstMoveIsRandom)
            {
                int validActionCount = Util.CountNonZero(_validActions);
                int selected = new Random().Next(validActionCount);
                int action = Util.FindNthNonZeroIndex(_validActions, selected + 1);
                return action;
            }

            var probs = _mcts.GetActionPolicy(state, playerTurn);
            Util.FilterProbsByValidActions(probs, _validActions);

            return Util.ArgMax(probs);
        }
    }
}
