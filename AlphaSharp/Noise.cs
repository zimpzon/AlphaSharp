using System;

namespace AlphaSharp
{
    internal static class Noise
    {
        public static void CreateDirichlet(float[] noiseTempArray, float amount)
        {
            // ]0..1[ for this implementation
            amount = Math.Min(amount, 0.9999f);
            amount = Math.Max(amount, 0.0001f);

            var random = new Random();

            float noiseSum = 0;

            for (int i = 0; i < noiseTempArray.Length; i++)
            {
                noiseTempArray[i] = SampleGamma(random, amount, 1);
                noiseSum += noiseTempArray[i];
            }

            for (int i = 0; i < noiseTempArray.Length; i++)
                noiseTempArray[i] /= noiseSum;  // Normalize noise to make it Dirichlet-distributed.
        }

        public static float SampleGamma(Random random, float shape, float scale)
        {
            // Implementation based on "A Simple Method for Generating Gamma Variables"
            // by George Marsaglia and Wai Wan Tsang. 2000.
            // https://dl.acm.org/doi/10.1145/358407.358414

            double d = shape + 1.0 / 3.0;
            double c = 1.0 / Math.Sqrt(9.0 * d);
            while (true)
            {
                double x;
                do
                {
                    x = Normal(random);
                } while (x <= -1);

                double v = 1.0 + c * x;
                v = v * v * v;
                double u = Uniform(random);
                x *= x;

                if (u < 1 - 0.0331 * x * x || Math.Log(u) < 0.5 * x + d * (1 - v + Math.Log(v)))
                    return (float)(scale * (d * v - 1.0 / 3.0));
            }
        }

        public static double Uniform(Random random)
        {
            return random.NextDouble();
        }

        public static double Normal(Random random)
        {
            // Box-Muller method.
            double u1 = Uniform(random);
            double u2 = Uniform(random);
            double r = Math.Sqrt(-2.0 * Math.Log(u1));
            double theta = 2.0 * Math.PI * u2;
            return r * Math.Sin(theta);
        }
    }
}
