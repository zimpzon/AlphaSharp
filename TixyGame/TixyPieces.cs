using AlphaSharp.Interfaces;

namespace TixyGame
{
    public static class TixyPieces
    {
        public static void DecodeAction(IGame game, byte[] state, int action, out int row, out int col, out int piece, out int dx, out int dy)
        {
            int planeSize = game.W * game.H;
            int planeId = action / planeSize;
            PlaneIdxToDeltas(planeId, out dx, out dy);

            int idxInPlane = action % planeSize;
            piece = state[idxInPlane];
            row = (idxInPlane % planeSize) / game.W;
            col = (idxInPlane % planeSize) % game.W;
        }

        public static byte FlipPlayer(byte piece)
        {
            if (piece == 0)
                return 0;

            return piece > 200 ? (byte)(piece - 100) : (byte)(piece + 100);
        }

        public static bool IsPlayer2(int piece)
            => piece > 200;

        public static Dictionary<int, char> PieceToChar => new()
        {
            [0] = '.',
            [P1.T] = 'T',
            [P1.I] = 'I',
            [P1.X] = 'X',
            [P1.Y] = 'Y',

            [P2.T] = 't',
            [P2.I] = 'i',
            [P2.X] = 'x',
            [P2.Y] = 'y'
        };

        public const int NumberOfPieces = 8;

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

        public static Dictionary<int, List<ValueTuple<int, int>>> PieceMoves => new()
        {
            [P1.T] = new List<ValueTuple<int, int>>() { (-1, 0), (0, -1), (1, 0), (0, 1) },
            [P1.I] = new List<ValueTuple<int, int>>() { (0, -1), (0, 1) },
            [P1.X] = new List<ValueTuple<int, int>>() { (-1, -1), (1, -1), (-1, 1), (1, 1) },
            [P1.Y] = new List<ValueTuple<int, int>>() { (-1, -1), (1, -1), (0, 1) },

            [P2.T] = new List<ValueTuple<int, int>>() { (-1, 0), (0, 1), (1, 0), (0, -1) },
            [P2.I] = new List<ValueTuple<int, int>>() { (0, -1), (0, 1) },
            [P2.X] = new List<ValueTuple<int, int>>() { (-1, -1), (1, -1), (-1, 1), (1, 1) },
            [P2.Y] = new List<ValueTuple<int, int>>() { (-1, 1), (1, 1), (0, -1) },
        };

        public static void PlaneIdxToDeltas(int planeIdx, out int dx, out int dy)
        {
            switch (planeIdx)
            {
                case 0: dx = 0; dy = -1; return;
                case 1: dx = 1; dy = -1; return;
                case 2: dx = 1; dy = 0; return;
                case 3: dx = 1; dy = 1; return;
                case 4: dx = 0; dy = 1; return;
                case 5: dx = -1; dy = 1; return;
                case 6: dx = -1; dy = 0; return;
                case 7: dx = -1; dy = -1; return;
            }

            throw new ArgumentOutOfRangeException(planeIdx.ToString());
        }

        public static int DeltasToPlaneIdx(int dx, int dy)
        {
            if (dx == 0 && dy == -1)
                return 0;
            else if (dx == 1 && dy == -1)
                return 1;
            else if (dx == 1 && dy == 0)
                return 2;
            else if (dx == 1 && dy == 1)
                return 3;
            else if (dx == 0 && dy == 1)
                return 4;
            else if (dx == -1 && dy == 1)
                return 5;
            else if (dx == -1 && dy == 0)
                return 6;
            else if (dx == -1 && dy == -1)
                return 7;

            throw new ArgumentOutOfRangeException($"{dx}, {dy}");
        }
    }
}
