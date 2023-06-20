using AlphaSharp;
using Xunit;

namespace TixyGame.Test
{
    public class GameTest
    {
        public class Mcts
        {
            [Fact]
            public void TestRun()
            {
                var game = new Tixy(5, 5);
                var skynet = new TixySkynet(game, new TixyParameters());

                var player1 = new MctsPlayer("ai", false, game, null);
                var player2 = new RandomPlayer(game);
                var oneVsOne = new OneVsOne(game, player1, player2);

                oneVsOne.Run();
            }
        }

        [Fact]
        public void FlipPlayer()
        {
            var game = new Tixy(7, 7);
            var state = new  byte[game.StateSize];
            Assert.All(state, i => Assert.Equal(0, i));

            // place left side top and bottom
            game.Set(state, 0, 0, TixyPieces.P2.X);

            game.Set(state, 6, 6, TixyPieces.P1.I);

            Assert.Equal(TixyPieces.P2.X, game.Get(state, 0, 0));
            Assert.Equal(TixyPieces.P1.I, game.Get(state, 6, 6));

            // flip player and verify it was flipped
            game.FlipStateToNextPlayer(state);

            Assert.Equal(TixyPieces.P2.I, game.Get(state, 0, 0));
            Assert.Equal(TixyPieces.P1.X, game.Get(state, 6, 6));
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
            var result = pit.Run();

            Assert.NotEqual(GameOver.Status.GameIsNotOver, result);
        }

        [Fact]
        public void TestRotate180()
        {
            var arr = new byte[] { 1, 2, 3, 4 };
            Util.Rotate180(arr, 2, 2);
            Assert.Equal(new byte[] { 4, 3, 2, 1 }, arr);
            Util.Rotate180(arr, 2, 2);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, arr);

            arr = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Util.Rotate180(arr, 3, 3);
            Assert.Equal(new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1 }, arr);
            Util.Rotate180(arr, 3, 3);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, arr);
        }
    }
}