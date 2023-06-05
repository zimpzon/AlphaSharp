namespace AlphaSharp
{
    public class Args
    {
        public bool ResumeFromCheckpoint = true;
        public bool ResumeFromEval = false;
        public int Iterations = 100;

        public int TrainingEpochs = 10;
        public int TrainingBatchSize = 64;
        public float TrainingLearningRate = 0.001f;

        public int selfPlayEpisodes = 10;
        public int SelfPlaySimulationCount = 100;
        public int SelfPlaySimulationMaxMoves = 100;
        public int SelfPlayEpisodeMaxMoves = 100;
        public int SelfPlayMaxExamples = 100000;

        public int EvalMaxMoves = 100;
        public int EvalSimulationMaxMoves = 100;
        public int EvalSimulationCount = 100;
        public float Cpuct = 1;
    }
}
