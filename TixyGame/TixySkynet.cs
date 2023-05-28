using AlphaSharp;

namespace TixyGame
{
    internal class TixySkynet : ISkynet
    {
        public void Suggest(byte[] currentState, byte[] suggestedActions, out float stateValue)
        {
            stateValue = 0.5f;
        }
    }
}
