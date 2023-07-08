namespace AlphaSharp.Interfaces
{
    public interface IPlayer
    {
        /// <summary>
        /// Given a state, pick an action to be executed. The state is always from the perspective of player one.
        /// </summary>
        int PickAction(byte[] state, int playerTurn);
        string Name { get; }
    }
}
