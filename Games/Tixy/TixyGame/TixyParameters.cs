﻿namespace TixyGame
{
    public class TixyParameters
    {
        /// <summary>
        /// Neural network training parameters.
        /// </summary>
        public int TrainingEpochs { get; set; } = 15;
        public int TrainingBatchSize { get; set; } = 32;
        public int TrainingBatchesPerEpoch { get; set; } = 500;
        public float TrainingLearningRate { get; set; } = 0.001f;
        public int TrainingMaxWorkerThreads { get; set; } = 1;
    }
}
