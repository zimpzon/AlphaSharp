namespace TixyGame
{
    public class GenericSkynetParam
    {
        /// <summary>
        /// Neural network training parameters.
        /// </summary>
        public int NumberOfPieces { get; set; } = -1;
        public int TrainingEpochs { get; set; } = 10;
        public int TrainingBatchSize { get; set; } = 32;
        public int TrainingBatchesPerEpoch { get; set; } = 500;
        public float TrainingLearningRate { get; set; } = 0.001f;
        public int TrainingMaxWorkerThreads { get; set; } = 1;
    }
}
