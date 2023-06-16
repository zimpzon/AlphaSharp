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
        public int MaxTrainingExamples { get; set; } = 500_000;
        public string OutputFolder { get; set; }
        public bool ResumeOnError { get; set; } = true;
        public bool SaveBackupAfterIteration { get; set; } = true;

        /// <summary>
        /// MCTS
        /// </summary>
        public int SimulationIterations { get; set; } = 100;
        public float Cpuct { get; set; } = 1;
        public float TemperatureThresholdMoves { get; set; } = 10;
        public float DirichletNoiseShape { get; set; } = 0.03f;
        public float DirichletNoiseAmount { get; set; } = 0.25f;

        /// <summary>
        /// Self-play parameters.
        /// </summary>
        public int SelfPlayEpisodes { get; set; } = 20;

        /// <summary>
        /// New model evaluation parameters.
        /// </summary>
        public int EvaluationRounds { get; set; } = 20;
        public EvaluationPlayers EvaluationPlayers { get; set; } = EvaluationPlayers.AlternatingModels;
    }
}
