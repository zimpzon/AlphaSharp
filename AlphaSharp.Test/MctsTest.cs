using AlphaSharp.Interfaces;
using System;
using System.Collections.Generic;
using Xunit;

namespace AlphaSharp.Test
{
    public class MctsTest
    {
        class TestGame : IGame
        {
            public int W => 3;

            public int H => 3;

            public int ActionCount => 1;

            public byte[] CreateEmptyActions()
            {
                return null;
            }

            public byte[] CreateEmptyState()
            {
                return null;
            }

            public void ExecutePlayerAction(byte[] state, int action)
            {
                throw new NotImplementedException();
            }

            public void FlipStateToNextPlayer(byte[] state)
            {
                throw new NotImplementedException();
            }

            public int GetGameEnded(byte[] state)
            {
                throw new NotImplementedException();
            }

            public List<byte[]> GetStateSymmetries(byte[] state)
            {
                throw new NotImplementedException();
            }

            public void GetValidActions(byte[] state, byte[] validActions)
            {
                throw new NotImplementedException();
            }

            public void PrintDisplayTextForAction(int action, Action<string> print)
            {
                throw new NotImplementedException();
            }

            public void PrintState(byte[] state, Action<string> print)
            {
                throw new NotImplementedException();
            }

            public void SetStartingState(byte[] state)
            {
                throw new NotImplementedException();
            }
        }

        class Skynet : ISkynet
        {
            public void Suggest(byte[] state, float[] actionsProbs, out float v)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void ssssss()
        {
            var game = new TestGame();
            var skynet = new Skynet();
            var mcts = new Mcts(game, skynet, new Args { cpuct = 1.0f, maxMCTSDepth = 100, numMCTSSims = 1000 });
        }
    }
}