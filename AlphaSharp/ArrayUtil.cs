using System;
using System.Numerics;

namespace AlphaSharp
{
    public static class ArrayUtil
    {
        public static int CountNonZero<T>(T[] arr) where T : INumber<T>
        {
            int result = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (!T.IsZero(arr[i]))
                    result++;
            }

            return result;
        }

        /// <summary>
        /// n is 1-based, result is 0-based
        /// </summary>
        public static int FindNthNonZeroIndex<T>(T[] arr, int n) where T : INumber<T>
        {
            if (n < 1)
                throw new ArgumentException($"n must be >= 1");

            int nonZeroCounter = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (!T.IsZero(arr[i]) && ++nonZeroCounter == n)
                    return i;
            }

            throw new ArgumentException($"cannot find non-zero value number {n} in array, found {nonZeroCounter} non-zero value(s)");
        }   
    }
}
