using AlphaSharp;
using static TixyGame.Tixy;

namespace TixyGame
{
    public class Tixy : IGame
    {
        public int W { get; }
        public int H { get; }
        public int ActionCount => W * H * MoveDirections;

        private const int MoveDirections = 8;
        private readonly byte[] _startingState;

        public static class Pieces
        {
            public static byte FlipPlayer(byte b)
            {
                if (b == 0)
                    return 0;

                return b > 200 ? (byte)(b - 100) : (byte)(b + 100);
            }

            public static bool IsPlayer2(int piece)
                => piece > 200;

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

        public Tixy(int w, int h)
        {
            W = w;
            H = h;

            _startingState = new byte[W * H];
            SetStartingPieces(_startingState);
        }

        public void ClearState()
        {
            for (int i = 0; i < _startingState.Length; i++)
                _startingState[i] = 0;
        }

        public void Set(byte[] state, int x, int y, byte value)
            => state[y * W + x] = value;

        public byte Get(byte[] state, int x, int y)
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

        public void GetValidActions(byte[] state, byte[] validActions)
        {
            for (int i = 0; i < W * H; i++)
            {
                int piece = state[i];
                if (piece == 0)
                    continue;

                int x = i % W;
                int y = i / W;
                int planeSize = W * H;
                int idxInPlane = i % planeSize;

                var pieceMoves = Util.PieceMoves[piece];
                foreach (var move in pieceMoves)
                {
                    int dx = move.Item1;
                    int dy = move.Item2;

                    if (x + dx >= 0 && x + dx < W && y + dy >= 0 && y + dy < H)
                    {
                        int pieceAtTargetLocation = state[(y + dy) * W + x + dx];
                        bool isLegalTarget = pieceAtTargetLocation == 0 || Pieces.IsPlayer2(pieceAtTargetLocation);
                        if (isLegalTarget)
                        {
                            int planeIdx = Util.DeltasToPlaneIdx(dx, dy);
                            validActions[planeSize * planeIdx + idxInPlane] = 1;
                        }
                    }
                }
            }
        }

        public void FlipStateToNextPlayer(byte[] state)
        {
            Util.Rotate180(state, W, H);

            for (int i = 0; i < state.Length; ++i)
                state[i] = Pieces.FlipPlayer(state[i]);
        }

        public void GetNextState(byte[] state, int action)
        {
            throw new NotImplementedException();
        }

        public List<byte[]> GetStateSymmetries(byte[] state)
        {
            return new List<byte[]> { state };
        }

        public static void Print(IGame game, byte[] state, Action<string> print)
        {
            // Define the mapping from numbers to characters
            var mapping = new Dictionary<int, char>
            {
                {0, '.'}, {101, 'T'}, {102, 'I'}, {103, 'X'}, {104, 'Y'}, {201, 't'}, {202, 'i'}, {203, 'x'}, {204, 'y'}
            };

            // Define the column labels
            char[] col_labels = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G' };

            // Print the column labels
            print("\n\n    " + string.Join(" ", col_labels.Take(game.W)));
            print("  +" + new string('-', 13) + "+");

            // Print each row with row number and border
            for (int y = 0; y < game.H; y++)
            {
                string row = (y + 1).ToString() + " | ";
                for (int x = 0; x < game.W; x++)
                {
                    int idx = y * game.W + x;
                    row += mapping[state[idx]] + " ";
                }
                row += "| " + (y + 1).ToString();
                print(row);
            }

            // Print the bottom border
            print("  +" + new string('-', 13) + "+");
            print("    " + string.Join(" ", col_labels.Take(game.W)));
            print("\n");
        }
    }
}
