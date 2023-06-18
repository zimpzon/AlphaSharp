using System;

namespace AlphaSharp
{
    public class TrainingData
    {
        public byte[] State { get; set; }
        public float[] ActionProbs { get; set; }
        public float ValueForPlayer1 { get; set; }

        public override string ToString()
            => $"ValueForCurrentPlayer: {ValueForPlayer1}";

        public TrainingData() { }

        public TrainingData(byte[] state, float[] actionProbs, float player1Score)
        {
            State = new byte[state.Length];
            Array.Copy(state, State, state.Length);

            ActionProbs = new float[actionProbs.Length];
            Array.Copy(actionProbs, ActionProbs, actionProbs.Length);

            ValueForPlayer1 = player1Score;
        }
    }
}
