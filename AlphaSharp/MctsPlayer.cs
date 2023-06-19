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

        public MctsPlayer(string name, bool firstMoveIsRandom, IGame game, ISkynet skynet, AlphaParameters args)
        {
            Name = name;
            _mcts = new Mcts(game, skynet, args);
            _game = game;
            _firstMoveIsRandom = firstMoveIsRandom;
        }

        public int PickAction(byte[] state, int playerTurn)
        {
            _moveCount++;

            if (_moveCount == 1 && _firstMoveIsRandom)
            {
                var validActions = new byte[_game.ActionCount];
                _game.GetValidActions(state, validActions);
                int validActionCount = ArrayUtil.CountNonZero(validActions);
                int selected = new Random().Next(validActionCount);
                int action = ArrayUtil.FindNthNonZeroIndex(validActions, selected + 1);
                return action;
            }

            var probs = _mcts.GetActionPolicy(state, playerTurn);
            return ArrayUtil.ArgMax(probs);
        }
    }
}
;