namespace AlphaSharp
{
    public interface IPlayer
    {
        int PickAction(byte[] state, byte[] validActions);
    }
}
