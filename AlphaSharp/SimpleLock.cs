using System.Threading;

namespace AlphaSharp
{
    internal static class SimpleLock
    {
        public static void AcquireLock(ref long l)
        {
            while (Interlocked.Exchange(ref l, 1) == 1)
                Thread.Sleep(0);
        }

        public static void ReleaseLock(ref long l)
        {
            Interlocked.Exchange(ref l, 0);
        }
    }
}
