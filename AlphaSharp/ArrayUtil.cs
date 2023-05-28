using System;
using System.Linq;
using System.Numerics;

namespace AlphaSharp
{
    public static class ArrayUtil
    {
        public static void Normalize(float[] values)
        {
            float sum = values.Sum();
            if (sum == 0)
                throw new ArgumentException("cannot normalize, sum of array is 0");

            for (int i = 0; i < values.Length; i++)
                values[i] /= sum;
        }

        public static void FilterProbsByValidActions(float[] probs, byte[] validActions)
        {
            for (int i = 0; i < probs.Length; i++)
                probs[i] *= validActions[i];
        }

        public static int CountNonZero<T>(T[] arr) where T : INumber<T>
            => arr.Count(v => !T.IsZero(v));

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
