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
        public bool OnlyTraining { get; set; } = false;
        public int Iterations { get; set; } = 1000;
        public int MaxWorkerThreads { get; set; } = 1;
        public int MaxTrainingExamples { get; set; } = 500_000;
        public string OutputFolder { get; set; }
        public bool SaveBackupAfterIteration { get; set; } = true;

        /// <summary>
        /// MCTS
        /// </summary>
        public int SelfPlaySimulationIterations { get; set; } = 100;
        public int EvalSimulationIterations { get; set; } = 100;
        public float CpuctSelfPlay { get; set; } = 1;
        public float CpuctEvaluation { get; set; } = 1;
        public float TemperatureThresholdMoves { get; set; } = 20;
        public float SelfPlaySleepCycleChance { get; set; } = 0.3f;
        public float SelfPlaySleepNoiseChance { get; set; } = 0.25f;
        public float DirichletNoiseShape { get; set; } = 1.0f;
        public float DirichletNoiseScale { get; set; } = 1.0f;
        public float RandomOutOfNowherePct { get; set; } = 0.99f;

        /// <summary>
        /// Self-play parameters.
        /// </summary>
        public int SelfPlayEpisodes { get; set; } = 20;
        public bool DeduplicateTrainingData { get; set; } = true;
        public float SampleDiscardPct { get; set; } = 0.0f;

        /// <summary>
        /// New model evaluation parameters.
        /// </summary>
        public int EvaluationRounds { get; set; } = 20;
        public EvaluationStyle EvaluationPlayers { get; set; } = EvaluationStyle.AlternatingModels;
        public bool DrawOptimalEvaluation { get; set; } = false; // Special evaluation for games where draws are optimal play
    }
}
