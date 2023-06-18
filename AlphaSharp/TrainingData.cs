using System;

namespace AlphaSharp
{
    public class TrainingData
    {
        public byte[] State { get; set; }
        public float[] ActionProbs { get; set; }
        public float ValueForCurrentPlayer { get; set; }
        public int PlayerTurn { get; set; }

        public override string ToString()
            => $"ValueForCurrentPlayer: {ValueForCurrentPlayer}, PlayerTurn: {PlayerTurn}";

        public TrainingData() { }

        public TrainingData(byte[] state, float[] actionProbs, float player1Score, int playerTurn)
        {
            State = new byte[state.Length];
            Array.Copy(state, State, state.Length);

            ActionProbs = new float[actionProbs.Length];
            Array.Copy(actionProbs, ActionProbs, actionProbs.Length);

            ValueForCurrentPlayer = player1Score;
            PlayerTurn = playerTurn;
        }
    }
}
