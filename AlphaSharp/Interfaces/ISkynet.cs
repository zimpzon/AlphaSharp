using System.Collections.Generic;
using static AlphaSharp.AlphaSharpTrainer;

namespace AlphaSharp.Interfaces
{
    public interface ISkynet
    {
        void LoadModel(string modelPath);
        void SaveModel(string modelPath);
        void Suggest(byte[] state, float[] dstActionsProbs, out float v);
        void Train(List<TrainingData> trainingData, TrainingProgressCallback progressCallback);
    }
}
