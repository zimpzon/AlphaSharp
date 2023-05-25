using System.Collections.Generic;

namespace AlphaSharp
{
    public interface IGame
    {
        int W { get; }
        int H { get; }
        byte[] GetStartingState();
        int ActionCount { get; }
        byte[] GetNextState(byte[] state, int action);
        byte[] GetValidActions(byte[] state);
        int GetGameEnded(byte[] state);
        byte[] FlipStateToOtherPlayer(byte[] state);
        List<byte[]> GetStateSymmetries(byte[] state);
    }
}
