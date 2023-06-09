namespace AlphaSharp
{
    public class TrainingData
    {
        public byte[] State { get; set; }
        public float[] ActionProbs { get; set; }
        public float Player1Value { get; set; }

        public TrainingData() { }

        public TrainingData(byte[] state, float[] actionProbs, float player1Score)
        {
            State = state;
            ActionProbs = actionProbs;
            Player1Value = player1Score;
        }
    }
}
