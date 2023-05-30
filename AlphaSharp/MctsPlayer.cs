using AlphaSharp.Interfaces;

namespace AlphaSharp
{
    public class MctsPlayer : IPlayer
    {
        private readonly IGame _game;
        private readonly ISkynet _skynet;
        private readonly Args _args;

        public MctsPlayer(IGame game, ISkynet skynet, Args args)
        {
            _game = game;
            _skynet = skynet;
            _args = args;
        }

        public int PickAction(byte[] state)
        {
            var mcts = new Mcts(_game, _skynet, _args);
            var probs = mcts.GetActionProbs(state, isTraining: false);
            return ArrayUtil.ArgMax(probs);
        }
    }
}
