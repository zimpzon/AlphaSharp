using System;

namespace AlphaSharp
{
    public class AlphaParameters
    {
        /// <summary>
        /// Information callbacks.
        /// </summary>
        public Action<LogLevel, string> TextInfoCallback { get; set; } =
            (logLevel, msg) => { Console.WriteLine($"[{logLevel}] {msg}"); };

        public Action<ProgressInfo> ProgressCallback { get; set; } =
            (p) => Console.WriteLine($"[{p.CurrentPhase}] {p.Progress * 100:0.00}% ({p.CurrentValue}/{p.Count}) {p.Elapsed}");

        /// <summary>
        /// Global parameters.
        /// </summary>
        public bool ResumeFromCheckpoint { get; set; } = true;
        public int Iterations { get; set; } = 1000;
        public int MaxWorkerThreads { get; set; } = 1;
        public int MaxTrainingExamples { get; set; } = 100000;
        public float Cpuct { get; set; } = 1;
        public string OutputFolder { get; set; }
        public bool ResumeOnError { get; set; } = true;  

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
