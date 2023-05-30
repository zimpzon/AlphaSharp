using AlphaSharp.Interfaces;
using TorchSharp;

namespace TixyGame
{
    public class TixySkynet : ISkynet
    {
        public void Suggest(byte[] currentState, float[] actionsProbs, out float v)
        {
            var rnd = new Random();
            for (int i = 0; i < actionsProbs.Length; i++)
                actionsProbs[i] = (float)rnd.NextDouble();

            // here state will be converted to 1-hot encoded. write directly to a tensor, if possible
            // or we need to create an array in this call, OR pass in a temp array to avoid miltithreading conflicts.
            //var t = torch.tensor(currentState, );
            v = 0.1f;
        }
    }
}
