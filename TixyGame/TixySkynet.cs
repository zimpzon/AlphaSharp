using AlphaSharp.Interfaces;
using TorchSharp;

namespace TixyGame
{
    internal class TixySkynet : ISkynet
    {
        public void Suggest(byte[] currentState, float[] actionsProbs, out float v)
        {
            // here state will be converted to 1-hot encoded. write directly to a tensor, if possible
            // or we need to create an array in this call, OR pass in a temp array to avoid miltithreading conflicts.
            //var t = torch.tensor(currentState, );
            v = 0;
        }
    }
}
