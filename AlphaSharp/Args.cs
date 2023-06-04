namespace AlphaSharp
{
    public class Args
    {
        public bool ResumeFromCheckpoint = true;
        public int Iterations = 100;

        public int TrainSelfPlayEpisodes = 10;

        public int TrainingEpochs = 10;
        public float TrainingLearningRate = 0.001f;
        public int TrainingSimulationCount = 100;
        public int TrainingSimulationMaxMoves = 100;
        public int TrainingEpisodeMaxMoves = 100;
        public int TrainingMaxExamples = 100000;
        public int TrainingBatchSize = 64;

        public int EvalMaxMoves = 100;
        public int EvalSimulationMaxMoves = 100;
        public int EvalSimulationCount = 100;
        public float Cpuct = 1;
    }
}
