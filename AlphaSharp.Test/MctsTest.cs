using AlphaSharp.Interfaces;
using System;
using System.Collections.Generic;
using TixyGame;
using Xunit;

namespace AlphaSharp.Test
{
    public class MctsTest
    {
        [Fact]
        public void ssssss()
        {
            var game = new Tixy(5, 5);
            var skynet = new TixySkynet();

            var mcts = new Mcts(game, skynet, new Args { cpuct = 1.0f, maxMCTSDepth = 100, numMCTSSims = 1000 });
            var state = game.CreateEmptyState();
            game.SetStartingState(state);

            mcts.GetActionProbs(state, isTraining: true, numberOfSim: 100, simMaxMoves: 100);
        }
    }
}