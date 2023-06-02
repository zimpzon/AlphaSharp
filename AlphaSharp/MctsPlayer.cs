using AlphaSharp.Interfaces;

namespace AlphaSharp
{
    public class MctsPlayer : IPlayer
    {
        private readonly Mcts _mcts;

        public MctsPlayer(IGame game, ISkynet skynet, Args args)
        {
            _mcts = new Mcts(game, skynet, args);
        }

        public int PickAction(byte[] state)
        {
            _mcts.Reset();

            var probs = _mcts.GetActionProbs(state, isTraining: false);
            return ArrayUtil.ArgMax(probs);
        }
    }
}
