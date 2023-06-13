namespace AlphaSharp
{
    public class TrainingData
    {
        public byte[] State { get; set; }
        public float[] ActionProbs { get; set; }
        public float ValueForPlayer1 { get; set; }

        public TrainingData() { }

        public TrainingData(byte[] state, float[] actionProbs, float player1Score)
        {
            State = state;
            ActionProbs = actionProbs;
            ValueForPlayer1 = player1Score;
        }
    }
}
