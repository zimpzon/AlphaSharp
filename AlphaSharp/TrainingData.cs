namespace AlphaSharp
{
    public class TrainingData
    {
        public byte[] State { get; set; }
        public float[] ActionProbs { get; set; }
        public float ValueForPlayer1 { get; set; }
        public int PlayerTurn { get; set; }

        public TrainingData() { }

        public TrainingData(byte[] state, float[] actionProbs, float player1Score, int playerTurn)
        {
            State = state;
            ActionProbs = actionProbs;
            ValueForPlayer1 = player1Score;
            PlayerTurn = playerTurn;
        }
    }
}
