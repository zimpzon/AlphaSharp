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
            var skynet = new TixySkynet(game, new Args());

            var player1 = new MctsPlayer(game, skynet, new Args());
            var player2 = new RandomPlayer(game);
            var oneVsOne = new OneVsOne(game, player1, player2);

            oneVsOne.Run(maxMoves: 100);
        }
    }
}