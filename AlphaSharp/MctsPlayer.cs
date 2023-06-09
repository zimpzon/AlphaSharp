﻿using AlphaSharp.Interfaces;

namespace AlphaSharp
{
    public class MctsPlayer : IPlayer
    {
        private readonly Mcts _mcts;

        public MctsPlayer(IGame game, ISkynet skynet, AlphaParameters args)
        {
            _mcts = new Mcts(game, skynet, args);
        }

        public int PickAction(byte[] state)
        {
            var probs = _mcts.GetActionProbs(state, isSelfPlay: false);
            return ArrayUtil.ArgMax(probs);
        }
    }
}
