using System.Collections.Generic;

namespace AlphaSharp.Interfaces
{
    public interface ISkynet
    {
        void LoadModel(string modelPath);
        void Suggest(byte[] state, float[] actionsProbs, out float v);
        void Train(List<TrainingData> trainingData, Args args, int iteration);
    }
}
