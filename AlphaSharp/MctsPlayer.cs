using AlphaSharp.Interfaces;

namespace AlphaSharp
{
    public class MctsPlayer : IPlayer
    {
        private readonly Mcts _mcts;

        public MctsPlayer(IGame game, ISkynet skynet, AlphaParameters args)
        {
            _mcts = new Mcts(game, skynet, args);
        }

        public int PickAction(byte[] state, int playerTurn)
        {
            var probs = _mcts.GetActionPolicy(state, playerTurn);
            return ArrayUtil.ArgMax(probs);
        }
    }
}
