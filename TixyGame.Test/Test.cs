using AlphaSharp;
using Xunit;

namespace TixyGame.Test
{
    public class Test
    {
        [Fact]
        public void FlipPlayer()
        {
            var game = new Tixy(5, 7);
            var state = game.CreateEmptyState();
            Assert.All(state, i => Assert.Equal(0, i));

            // place left side top and bottom
            game.Set(state, 0, 0, TixyPieces.P2.X);
            game.Set(state, 1, 0, TixyPieces.P2.Y);
            game.Set(state, 0, 6, TixyPieces.P1.I);
            game.Set(state, 1, 6, TixyPieces.P1.T);

            Assert.Equal(TixyPieces.P2.X, game.Get(state, 0, 0));
            Assert.Equal(TixyPieces.P1.I, game.Get(state, 0, 6));

            // flip player and verify it was flipped
            game.FlipStateToNextPlayer(state);

            Assert.Equal(TixyPieces.P2.I, game.Get(state, 0, 0));
            Assert.Equal(TixyPieces.P1.X, game.Get(state, 0, 6));
        }

        [Fact]
        public void ValidActions()
        {
            var game = new Tixy(5, 7);
            var state = game.CreateEmptyState();

            // place left side top and bottom
            game.Set(state, 0, 0, TixyPieces.P2.X);
            game.Set(state, 1, 0, TixyPieces.P2.Y);

            game.Set(state, 0, 6, TixyPieces.P1.I);
            game.Set(state, 1, 6, TixyPieces.P1.T);

            var actions = game.CreateEmptyActions();
            Assert.Equal(0, actions.Count(a => a > 0));

            game.GetValidActions(state, actions);
            Assert.NotEqual(0, actions.Count(a => a > 0));

            // move P1 T up-right, magic number 6 is the x pos of P1 T
            const int p1_T_XPos = 6;
            Assert.Equal(TixyPieces.P1.T, game.Get(state, 1, p1_T_XPos));

            // verify that moving south east with piece at 0,0 is valid
            int northEastPlaneIdx = TixyPieces.DeltasToPlaneIdx(1, -1); // 1, -1 is north east
            int planeSize = game.W * game.H;
            int action = northEastPlaneIdx * planeSize + p1_T_XPos * game.W + 1;

            // verify the action was set to 1 when calling GetValidActions
            Assert.Equal(1, actions[action]);

            // execute the action and verify the piece was moved
            game.ExecutePlayerAction(state, action);
            Assert.Equal(0, game.Get(state, 1, p1_T_XPos));

            int p1_T_new_XPos = 5;
            Assert.Equal(TixyPieces.P1.T, game.Get(state, 2, p1_T_new_XPos));
        }

        [Fact]
        public void OneVsOne()
        {
            // make sure 1vs1 does not hang, crash, or the like
            var player1 = new RandomPlayer();
            var player2 = new RandomPlayer();

            var game = new Tixy(5, 7);
            var pit = new OneVsOne(game, player1, player2);

            var state = game.CreateEmptyState();
            var actions = game.CreateEmptyActions();

            int result = pit.Run(1000, state, actions);
            Assert.InRange(result, -1, 1);
        }
    }
}