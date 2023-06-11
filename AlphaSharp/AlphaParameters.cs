using System;

namespace AlphaSharp
{
    public class AlphaParameters
    {
        public AlphaParameters()
        {
            TextInfoCallback = (logLevel, msg) => { DefaultLogging.Log(logLevel, MaxLogLevel, LogTimestamps, msg); };
            ProgressCallback = (progress, additionalInfo) => { DefaultLogging.LogProgress(progress, LogTimestamps, additionalInfo); };
        }

        /// <summary>
        /// Callbacks for logging and progress.
        /// </summary>
        public Action<LogLevel, string> TextInfoCallback { get; set; }
        public Action<ProgressInfo, string> ProgressCallback { get; set; }
        public LogLevel MaxLogLevel { get; set; } = LogLevel.Info;
        public bool LogTimestamps { get; set; } = true;

        /// <summary>
        /// Global parameters.
        /// </summary>
        public bool ResumeFromCheckpoint { get; set; } = true;
        public int Iterations { get; set; } = 1000;
        public int MaxWorkerThreads { get; set; } = 1;
        public int MaxTrainingExamples { get; set; } = 100000;
        public float Cpuct { get; set; } = 1;
        public float DirichletNoiseShape = 0.03f;
        public float DirichletNoiseAmount = 0.25f;
        public string OutputFolder { get; set; }
        public bool ResumeOnError { get; set; } = true;
        public bool SaveBackupAfterIteration { get; set; } = true;

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
        public EvaluationPlayers EvaluationPlayers { get; set; } = EvaluationPlayers.NewModelAlternating;

        /// <summary>
        /// New model evaluation parameters, MCTS simulation.
        /// </summary>
        public int EvaluationSimulationCount { get; set; } = 100;
        public int EvaluationSimulationMaxMoves { get; set; } = 100;
    }
}
