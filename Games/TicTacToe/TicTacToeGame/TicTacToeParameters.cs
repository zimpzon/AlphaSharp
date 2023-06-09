namespace TicTacToeGame
{
    public class TicTacToeParameters
    {
        /// <summary>
        /// Neural network training parameters.
        /// </summary>
        public int TrainingEpochs { get; set; } = 10;
        public int TrainingBatchSize { get; set; } = 64;
        public float TrainingLearningRate { get; set; } = 0.001f;
    }
}
