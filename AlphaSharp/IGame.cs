using System.Collections.Generic;

namespace AlphaSharp
{
    public interface IGame
    {
        int W { get; }
        int H { get; }
        byte[] GetStartingState();
        int ActionCount { get; }
        void GetValidActions(byte[] state, byte[] validActions);
        int GetGameEnded(byte[] state);
        void GetNextState(byte[] state, int action);
        void FlipStateToNextPlayer(byte[] state);
        List<byte[]> GetStateSymmetries(byte[] state);
    }
}
