using AlphaSharp.Interfaces;
using System;
using System.Linq;

namespace AlphaSharp
{
    public class MctsPlayer : IPlayer
    {
        public string Name { get; }

        private readonly bool _makeFirstMoveRandom;
        private readonly Mcts _mcts;
        private readonly IGame _game;
        private int _moveCount;

        public MctsPlayer(string name, bool makeFirstMoveRandom, IGame game, ISkynet skynet, AlphaParameters args)
        {
            Name = name;
            _makeFirstMoveRandom = makeFirstMoveRandom;
            _mcts = new Mcts(game, skynet, args);
            _game = game;
        }

        public int PickAction(byte[] state, int playerTurn)
        {
            _moveCount++;

            if (_moveCount == 1)
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
