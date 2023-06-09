using System;

namespace AlphaSharp
{
    public class ProgressInfo
    {
        public enum Phase
        {
            Iterations,
            SelfPlayEpisodes,
            TrainingEpochs,
            EvaluationRounds,
        }

        public Phase CurrentPhase { get; private set; }
        public int Count { get; private set; }
        public int CurrentValue { get; private set; }
        public float Progress { get; private set; }
        public DateTimeOffset StartTimeUtc { get; private set; }
        public TimeSpan Elapsed { get; set; }

        public ProgressInfo Completed()
            => Update(Count);

        public ProgressInfo Update(int currentValue)
        {
            CurrentValue = currentValue;
            Progress = (float)CurrentValue / Count;
            Elapsed = DateTimeOffset.UtcNow - StartTimeUtc;
            return this;
        }

        public static ProgressInfo Create(Phase phase, int count, int current = 0)
        {
            return new ProgressInfo
            {
                CurrentPhase = phase,
                Count = count,
                CurrentValue = current,
                StartTimeUtc = DateTimeOffset.UtcNow,
            };
        }
    }
}
