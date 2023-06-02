namespace AlphaSharp
{
    public class Args
    {
        public int SelfPlayIterations = 10;
        public int SelfPlayEpisodes = 10;

        public int TrainingSimulationCount = 100;
        public int TrainingSimulationMaxMoves = 100;
        public int TrainingEpisodeMaxMoves = 100;
        public int TrainingMaxExamples = 100000;
        public int TrainingBatchSize = 64;

        public int PlayingSimulationCount = 100;
        public int PlayingSimulationMaxMoves = 100;
        public float Cpuct = 1;
    }
}
