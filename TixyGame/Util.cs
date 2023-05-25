using System.Collections.Concurrent;

namespace TixyGame
{
    public static class Util
    {
        public static readonly ConcurrentDictionary<int, List<ValueTuple<int, int>>> PieceMoves = new()
        {
            [Tixy.Pieces.P1.T] = new List<ValueTuple<int, int>>() { (-1, -1), (0, -1), (1, -1), (0, 1) },
            [Tixy.Pieces.P1.I] = new List<ValueTuple<int, int>>() { (0, -1), (0, 1) },
            [Tixy.Pieces.P1.X] = new List<ValueTuple<int, int>>() { (-1, -1), (1, -1), (-1, 1), (1, 1) },
            [Tixy.Pieces.P1.Y] = new List<ValueTuple<int, int>>() { (-1, -1), (1, -1), (0, 1) },

            [Tixy.Pieces.P2.T] = new List<ValueTuple<int, int>>() { (0, -1), (0, 1), (-1, 1), (1, 1) },
            [Tixy.Pieces.P2.I] = new List<ValueTuple<int, int>>() { (0, -1), (0, 1) },
            [Tixy.Pieces.P2.X] = new List<ValueTuple<int, int>>() { (-1, -1), (1, -1), (-1, 1), (1, 1) },
            [Tixy.Pieces.P2.Y] = new List<ValueTuple<int, int>>() { (-1, 1), (1, 1), (0, -1) },
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

        public static void Rotate180(byte[] arr, int w, int h)
        {
            // flip x-axis
            for (int y = 0; y < h / 2; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    int idxFlipped = (h - y - 1) * w + x;

                    byte temp = arr[idx];
                    arr[idx] = arr[idxFlipped];
                    arr[idxFlipped] = temp;
                }
            }
        }
    }
}
