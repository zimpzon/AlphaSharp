namespace TixyGame
{
    public static class Util
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0180:Use tuple to swap values", Justification = "meh, I like my temp variable")]
        public static void Rotate180(byte[] arr, int w, int h)
        {
            // mirror x-axis in the middle of the arr
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
