namespace AlphaSharp.Interfaces
{
    public interface ISkynet
    {
        void Suggest(byte[] state, float[] actionsProbs, out float v);
    }
}
