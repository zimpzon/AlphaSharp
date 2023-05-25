using System.Diagnostics;
using Xunit;

namespace TixyGame.Test
{
    public class Test
    {
        [Fact]
        public void FlipPlayer()
        {
            var game = new Tixy(5, 7);
            game.ClearState();
            var state = game.GetStartingState();
            Assert.All(state, i => Assert.Equal(0, i));

            // place left side top and bottom
            game.Set(state, 0, 0, Tixy.Pieces.P2.X);
            game.Set(state, 1, 0, Tixy.Pieces.P2.Y);

            game.Set(state, 0, 6, Tixy.Pieces.P1.I);
            game.Set(state, 1, 6, Tixy.Pieces.P1.T);

            Assert.Equal(Tixy.Pieces.P2.X, game.Get(state, 0, 0));
            Assert.Equal(Tixy.Pieces.P1.I, game.Get(state, 0, 6));

            // flip player and verify it was flipped
            game.FlipStateToNextPlayer(state);

            Assert.Equal(Tixy.Pieces.P2.I, game.Get(state, 0, 0));
            Assert.Equal(Tixy.Pieces.P1.X, game.Get(state, 0, 6));
        }

        [Fact]
        public void ValidActions()
        {
            var game = new Tixy(5, 7);
            game.ClearState();
            var state = game.GetStartingState();
            Assert.All(state, i => Assert.Equal(0, i));

            // place left side top and bottom
            game.Set(state, 0, 0, Tixy.Pieces.P2.X);
            game.Set(state, 1, 0, Tixy.Pieces.P2.Y);

            game.Set(state, 0, 6, Tixy.Pieces.P1.I);
            game.Set(state, 1, 6, Tixy.Pieces.P1.T);

            var validActions = new byte[game.ActionCount];
            int validCount = validActions.Count(a => a > 0);
            Assert.Equal(0, validCount);

            game.GetValidActions(state, validActions);
            validCount = validActions.Count(a => a > 0);
        }
    }
}