using AlphaSharp.Interfaces;
using System.Text;

namespace TixyGame
{
    public class Tixy : IGame
    {
        public int W { get; }
        public int H { get; }
        public int ActionCount => W * H * MoveDirections;
        public int StateSize => W * H;

        private const int MoveDirections = 8;

        public Tixy(int w, int h)
        {
            W = w;
            H = h;
        }

        public static void ClearPieces(byte[] state)
            => Array.Clear(state, 0, state.Length);

        public void Set(byte[] state, int x, int y, byte value)
            => state[y * W + x] = value;

        public byte Get(byte[] state, int x, int y)
            => state[y * W + x];

        public void SetStartingState(byte[] state)
        {
            ClearPieces(state);

            Set(state, 0, 0, TixyPieces.P2.T);
            Set(state, 1, 0, TixyPieces.P2.I);
            Set(state, 2, 0, TixyPieces.P2.X);
            Set(state, 3, 0, TixyPieces.P2.Y);

            Set(state, 0, H - 1, TixyPieces.P1.T);
            Set(state, 1, H - 1, TixyPieces.P1.I);
            Set(state, 2, H - 1, TixyPieces.P1.X);
            Set(state, 3, H - 1, TixyPieces.P1.Y);
        }

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

                    bool isQueenP1 = piece == TixyPieces.P1.I;
                    bool isQueenP2 = piece == TixyPieces.P2.I;

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
            Array.Clear(validActions);

            for (int i = 0; i < W * H; i++)
            {
                int piece = state[i];
                if (piece == 0 || TixyPieces.IsPlayer2(piece))
                    continue;

                int x = i % W;
                int y = i / W;
                int planeSize = W * H;
                int idxInPlane = i % planeSize;

                var pieceMoves = TixyPieces.PieceMoves[piece];
                foreach (var move in pieceMoves)
                {
                    int dx = move.Item1;
                    int dy = move.Item2;

                    if (x + dx >= 0 && x + dx < W && y + dy >= 0 && y + dy < H)
                    {
                        int pieceAtTargetLocation = state[(y + dy) * W + x + dx];

                        bool isLegalTarget = pieceAtTargetLocation == 0 || TixyPieces.IsPlayer2(pieceAtTargetLocation);
                        if (isLegalTarget)
                        {
                            int planeIdx = TixyPieces.DeltasToPlaneIdx(dx, dy);
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
                state[i] = TixyPieces.FlipPlayer(state[i]);
        }

        public void ExecutePlayerAction(byte[] state, int action)
        {
            int planeId = action / (W * H);
            TixyPieces.PlaneIdxToDeltas(planeId, out int dx, out int dy);

            int idxInLayer = action % (W * H);
            byte piece = state[idxInLayer];
            if (piece == 0)
                throw new ArgumentException("Invalid action, piece is 0");

            int dstIdxInLayer = idxInLayer + dx + dy * W;

            //int dstPiece = state[dstIdxInLayer];
            //if (TixyPieces.IsPlayer2(dstPiece))
            //    Debug.WriteLine($"capture: {dstPiece}");

            state[dstIdxInLayer] = piece;
            state[idxInLayer] = 0;
        }

        public List<byte[]> GetStateSymmetries(byte[] state)
        {
            return new List<byte[]> { state };
        }

        public void PrintDisplayTextForAction(int action, Action<string> print)
        {
            int planeSize = W * H;
            int planeId = action / planeSize;
            TixyPieces.PlaneIdxToDeltas(planeId, out int dx, out int dy);
            int idxInPlane = action % planeSize;

            int x1 = idxInPlane % W;
            int y1 = idxInPlane / W;
            int x2 = x1 + dx;
            int y2 = y1 + dy;

            char fromLetter = (char)('A' + x1);
            char fromNumber = (char)('1' + y1);
            char toLetter = (char)('A' + x2);
            char toNumber = (char)('1' + y2);

            print($"moving {fromLetter}{fromNumber} to {toLetter}{toNumber}\n");
        }

        public void PrintState(byte[] state, Action<string> print)
        {
            string letterRow = string.Join(" ", Enumerable.Range(0, W).Select(i => $"{(char)('A' + i)}"));

            print($"\n    {letterRow}");

            print("  +" + new string('-', 13) + "+");

            var sb = new StringBuilder();

            for (int y = 0; y < H; y++)
            {
                sb.Clear();
                sb.Append($"{y + 1} | ");
                for (int x = 0; x < W; x++)
                {
                    int idx = y * W + x;
                    sb.Append($"{TixyPieces.PieceToChar[state[idx]]} ");
                }
                sb.Append($"| {y + 1}");
                print(sb.ToString());
            }

            print("  +" + new string('-', 13) + "+");
            print($"    {letterRow}");
        }
    }
}
