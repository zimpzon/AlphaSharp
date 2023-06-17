using AlphaSharp.Interfaces;

namespace AlphaSharp
{
    public class RandomPlayer : IPlayer
    {
        private readonly IGame _game;

        public RandomPlayer(IGame game)
        {
            _game = game;
        }

        public int PickAction(byte[] state, int _)
        {
            var validActions = new byte[_game.ActionCount];
            _game.GetValidActions(state, validActions);

            int randomAction = ActionUtil.PickRandomNonZeroAction(validActions);
            return randomAction;
        }
    }
}
