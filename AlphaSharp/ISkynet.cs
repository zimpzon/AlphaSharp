namespace AlphaSharp
{
    public interface ISkynet
    {
        void Suggest(float[] currentState, float[] actionsProbs, out float v);
    }
}
