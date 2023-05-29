using AlphaSharp.Interfaces;

namespace AlphaSharp
{
    public class RandomPlayer : IPlayer
    {
        public int PickAction(byte[] state, byte[] validActions)
        {
            int randomAction = ActionUtil.PickRandomNonZeroAction(validActions);
            return randomAction;
        }
    }
}
