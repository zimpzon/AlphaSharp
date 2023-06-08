﻿namespace AlphaSharp
{
    public class AlphaParameters
    {
        /// <summary>
        /// Global parameters.
        /// </summary>
        public bool ResumeFromCheckpoint { get; set; } = true;
        public bool ResumeFromEval { get; set; } = false;
        public int Iterations { get; set; } = 1000;
        public int MaxWorkerThreads { get; set; } = 1;
        public int MaxTrainingExamples { get; set; } = 100000;
        public float Cpuct { get; set; } = 1;
        public string OutputFolder { get; set; }

        /// <summary>
        /// Self-play parameters.
        /// </summary>
        public int SelfPlayEpisodes { get; set; } = 20;
        public int SelfPlayEpisodeMaxMoves { get; set; } = 100;

        /// <summary>
        /// Self-play parameters, MCTS simulation.
        /// </summary>
        public int SelfPlaySimulationCount { get; set; } = 100;
        public int SelfPlaySimulationMaxMoves { get; set; } = 100;

        /// <summary>
        /// New model evaluation parameters.
        /// </summary>
        public int EvaluationRounds { get; set; } = 20;
        public int EvaluationMaxMoves { get; set; } = 100;

        /// <summary>
        /// New model evaluation parameters, MCTS simulation.
        /// </summary>
        public int EvaluationSimulationCount { get; set; } = 100;
        public int EvaluationSimulationMaxMoves { get; set; } = 100;
    }
}
