using System;
using System.Collections.Generic;

namespace AlphaSharp.Interfaces
{
    public interface IGame
    {
        int W { get; }
        int H { get; }
        int ActionCount { get; }
        int StateSize { get; }
        void SetStartingState(byte[] state);
        void GetValidActions(byte[] state, byte[] validActions);
        int GetGameEnded(byte[] state);
        void ExecutePlayerAction(byte[] state, int action);
        void FlipStateToNextPlayer(byte[] state);
        List<byte[]> GetStateSymmetries(byte[] state);

        void PrintState(byte[] state, Action<string> print);
        void PrintDisplayTextForAction(int action, Action<string> print);
    }
}
