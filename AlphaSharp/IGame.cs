using System;
using System.Collections.Generic;

namespace AlphaSharp
{
    public interface IGame
    {
        int W { get; }
        int H { get; }
        byte[] CreateEmptyState();
        byte[] CreateEmptyActions();
        void SetStartingState(byte[] state);
        int ActionCount { get; }
        void GetValidActions(byte[] state, byte[] validActions);
        int GetGameStatus(byte[] state);
        void ExecutePlayerAction(byte[] state, int action);
        void FlipStateToNextPlayer(byte[] state);
        List<byte[]> GetStateSymmetries(byte[] state);

        void PrintState(byte[] state, Action<string> print);
        void PrintDisplayTextForAction(int action, Action<string> print);
    }
}
