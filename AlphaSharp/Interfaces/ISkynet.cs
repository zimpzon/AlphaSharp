using System.Collections.Generic;

namespace AlphaSharp.Interfaces
{
    public interface ISkynet
    {
        void LoadModel(string modelPath);
        void SaveModel(string modelPath);
        void Suggest(byte[] state, float[] dstActionsProbs, out float v);
        void Train(List<TrainingData> trainingData, Args args, int iteration);
    }
}
