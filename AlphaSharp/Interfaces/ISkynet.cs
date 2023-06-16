using System.Collections.Generic;
using static AlphaSharp.AlphaSharpTrainer;

namespace AlphaSharp.Interfaces
{
    public interface ISkynet
    {
        void LoadModel(string modelPath);
        void SaveModel(string modelPath);
        void Suggest(byte[] state, int playerTurn, float[] dstActionsProbs, out float v);

        /// <summary>
        /// In many games you should add info about current player when training. Ex in a game where player 1 or 2 can always win, you want different behaviour depending on player.
        /// </summary>
        void Train(List<TrainingData> trainingData, TrainingProgressCallback progressCallback);
    }
}
