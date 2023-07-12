using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AlphaSharp
{
    public static class Util
    {
        public static IEnumerable<int> RepeatSequence(int maxValue, int total)
        {
            int v = 0;
            for (int i = 0; i < total; ++i)
            {
                yield return v;

                if (++v >= maxValue)
                    v = 0;
            }
        }

        public static void Shuffle<T>(List<T> list)
        {
            var rnd = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static int WeightedChoice(Random random, float[] probabilities)
        {
            var total = probabilities.Sum();
            var randomValue = random.NextDouble() * total;

            for (int i = 0; i < probabilities.Length; i++)
            {
                randomValue -= probabilities[i];
                if (randomValue <= 0)
                    return i;
            }

            // This should never happen if the probabilities sum to 1
            throw new InvalidOperationException("Invalid probabilities");
        }

        public static float[] Softmax(float[] values, float temperature)
        {
            // Subtract max for numerical stability
            float maxProb = values.Max();
            for (int i = 0; i < values.Length; i++)
                values[i] -= maxProb;

            // Apply temperature and exponentiate
            for (int i = 0; i < values.Length; i++)
                values[i] = (float)Math.Exp(values[i] / temperature);

            Normalize(values);
            return values;
        }

        public static int ArgMax(float[] arr)
        {
            if (arr.Length == 0)
                throw new ArgumentException("The input array cannot be empty.");

            int maxIndex = 0;
            float maxValue = arr[0];

            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i] > maxValue)
                {
                    maxValue = arr[i];
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        public static void Normalize(float[] values)
        {
            double sum = values.Sum();
            if (sum == 0)
                throw new ArgumentException("cannot normalize, sum of array is 0");

            for (int i = 0; i < values.Length; i++)
                values[i] = (float)(values[i] / sum);
        }

        public static void FilterProbsByValidActions(float[] probs, byte[] validActions)
        {
            for (int i = 0; i < probs.Length; i++)
                probs[i] *= validActions[i];
        }

        public static void Add(float[] values, float value)
        {
            for (int i = 0; i < values.Length; i++)
                values[i] += value;
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
