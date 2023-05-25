using Microsoft.VisualStudio.TestPlatform.Utilities;
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

            // move P1 T up-right
            Assert.Equal(Tixy.Pieces.P1.T, game.Get(state, 1, 6));

            // verify that moving south east with piece at 0,0 is valid
            int northEastPlaneIdx = Util.DeltasToPlaneIdx(1, -1); // 1, -1 is north east
            int planeSize = game.W * game.H;
            int action = northEastPlaneIdx * planeSize + 6 * game.W + 1;

            // verify the action was set to 1 when calling GetValidActions
            Assert.Equal(1, validActions[action]);

            // execute the action and verify the piece was moved
            game.GetNextState(state, action);
            Assert.Equal(0, game.Get(state, 1, 6));
            Assert.Equal(Tixy.Pieces.P1.T, game.Get(state, 2, 5));
        }
    }
}