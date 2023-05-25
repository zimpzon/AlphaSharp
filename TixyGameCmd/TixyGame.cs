using AlphaSharp;

namespace TixyGameCmd
{
    internal class TixyGame : IGame
    {
        public int W { get; }
        public int H { get; }
        public int ActionCount => W * H * MoveDirections;

        private const int MoveDirections = 8;
        private readonly byte[] _startingState;

        private static class Pieces
        {
            public static byte FlipPlayer(byte b)
            {
                if (b == 0)
                    return 0;

                return b > 200 ? (byte)(b - 100) : (byte)(b + 100);
            }

            public static class P1
            {
                public const byte T = 101;
                public const byte I = 102;
                public const byte X = 103;
                public const byte Y = 104;
            }

            public static class P2
            {
                public const byte T = 201;
                public const byte I = 202;
                public const byte X = 203;
                public const byte Y = 204;
            }
        }

        public TixyGame(int w, int h)
        {
            W = w;
            H = h;

            _startingState = new byte[W * H];
            SetStartingPieces(_startingState);
        }

        private void Set(byte[] state, int x, int y, byte value)
            => state[y * W + x] = value;

        private byte Get(byte[] state, int x, int y)
            => state[y * W + x];

        private void SetStartingPieces(byte[] state)
        {
            Set(state, 0, 0, Pieces.P2.T);
            Set(state, 1, 0, Pieces.P2.I);
            Set(state, 2, 0, Pieces.P2.X);
            Set(state, 3, 0, Pieces.P2.Y);

            Set(state, 0, H - 1, Pieces.P1.T);
            Set(state, 1, H - 1, Pieces.P1.I);
            Set(state, 2, H - 1, Pieces.P1.X);
            Set(state, 3, H - 1, Pieces.P1.Y);
        }

        public byte[] GetStartingState()
            => _startingState;

        public int GetGameEnded(byte[] state)
        {
            int countQueenP1AtTop = 0;
            int countQueenP2AtBottom = 0;
            int countQueenP1 = 0;
            int countQueenP2 = 0;

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    byte piece = Get(state, x, y);

                    bool isQueenP1 = piece == Pieces.P1.I;
                    bool isQueenP2 = piece == Pieces.P2.I;

                    countQueenP1AtTop += isQueenP1 && y == 0 ? 1 : 0;
                    countQueenP2AtBottom += isQueenP2 && y == H - 1 ? 1 : 0;
                    countQueenP1 += isQueenP1 ? 1 : 0;
                    countQueenP2 += isQueenP2 ? 1 : 0;
                }
            }

            bool p1Wins = countQueenP1AtTop > 0 || countQueenP2 == 0;
            if (p1Wins)
                return 1;

            bool p2Wins = countQueenP2AtBottom > 0 || countQueenP1 == 0;
            if (p2Wins)
                return 1;

            return 0;
        }

        public byte[] GetValidActions(byte[] state)
        {
            throw new NotImplementedException();
        }

        public byte[] FlipStateToOtherPlayer(byte[] state)
        {
            // Currently in-place!
            Array.Reverse(state);
            for (int i = 0; i < state.Length; ++i)
                state[i] = Pieces.FlipPlayer(state[i]);

            return state;
        }

        public byte[] GetNextState(byte[] state, int action)
        {
            // Currently in-place!
            throw new NotImplementedException();
        }

        public List<byte[]> GetStateSymmetries(byte[] state)
        {
            return new List<byte[]> { state };
        }
    }
}
