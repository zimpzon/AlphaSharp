using AlphaSharp;
using Xunit;

namespace TixyGame.Test
{
    public class GameTest
    {
        [Fact]
        public void FlipPlayer()
        {
            var game = new Tixy(7, 7);
            var state = new  byte[game.StateSize];
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
            var state = new byte[game.StateSize];

            // place left side top and bottom
            game.Set(state, 0, 0, TixyPieces.P2.X);
            game.Set(state, 1, 0, TixyPieces.P2.Y);

            game.Set(state, 0, 6, TixyPieces.P1.I);
            game.Set(state, 1, 6, TixyPieces.P1.T);

            var actions = new byte[game.ActionCount];
            Assert.Equal(0, actions.Count(a => a > 0));

            game.GetValidActions(state, actions);
            Assert.NotEqual(0, actions.Count(a => a > 0));

            // move P1 T up
            const int p1_T_YPos = 6;
            Assert.Equal(TixyPieces.P1.T, game.Get(state, 1, p1_T_YPos));

            int northPlaneIdx = TixyPieces.DeltasToPlaneIdx(0, -1); // 0, -1 is north
            int planeSize = game.W * game.H;
            int action = northPlaneIdx * planeSize + p1_T_YPos * game.W + 1;

            // verify the action was set to 1 when calling GetValidActions
            Assert.Equal(1, actions[action]);

            // execute the action and verify the piece was moved
            game.ExecutePlayerAction(state, action);
            Assert.Equal(0, game.Get(state, 1, p1_T_YPos));

            int p1_T_new_YPos = p1_T_YPos - 1;
            Assert.Equal(TixyPieces.P1.T, game.Get(state, 1, p1_T_new_YPos));
        }

        [Fact]
        public void OneVsOne()
        {
            var game = new Tixy(5, 7);

            var player1 = new RandomPlayer(game);
            var player2 = new RandomPlayer(game);

            var pit = new OneVsOne(game, player1, player2);
            int result = pit.Run(1000);

            Assert.InRange(result, -1, 1);
        }
    }
}