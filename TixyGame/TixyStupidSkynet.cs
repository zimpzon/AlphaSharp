using AlphaSharp;
using AlphaSharp.Interfaces;

namespace TixyGame
{
    public class TixyStupidSkynet : ISkynet
    {
        public TixyStupidSkynet(IGame _)
        {
        }

        public void LoadModel(string modelPath)
        {
            throw new NotImplementedException();
        }

        public void Suggest(byte[] state, float[] actionsProbs, out float v)
        {
            float val = 1.0f / actionsProbs.Length;
            for (int i = 0; i < actionsProbs.Length; i++)
            {
                actionsProbs[i] = val;
            }

            v = 0.5f;
        }

        public void Train(List<TrainingData> trainingData, Args args, int iteration)
        {
            throw new NotImplementedException();
        }
    }
}
