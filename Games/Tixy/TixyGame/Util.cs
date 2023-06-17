namespace TixyGame
{
    public static class Util
    {
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0180:Use tuple to swap values", Justification = "meh, I like my temp variable")]
        public static void Rotate180(byte[] arr, int w, int h)
        {
            for (int i = 0; i < arr.Length / 2; i++)
            {
                int x = i % w;
                int y = i / w;
                int rotY = h - y - 1;
                int rotX = w - x - 1;
                int rotIdx = rotY * w + rotX;
                byte rotVal = arr[rotIdx];
                arr[rotIdx] = arr[i];
                arr[i] = rotVal;
            }
        }
    }
}
