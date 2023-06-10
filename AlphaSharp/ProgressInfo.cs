using System;

namespace AlphaSharp
{
    public class ProgressInfo
    {
        public enum Phase
        {
            Iteration,
            SelfPlay,
            Train,
            Eval,
        }

        public Phase CurrentPhase { get; private set; }
        public int NumberOfValues { get; private set; }
        public int CurrentValue { get; private set; }
        public float Progress { get; private set; }
        public DateTimeOffset StartTimeUtc { get; private set; }
        public TimeSpan Elapsed { get; set; }

        public ProgressInfo Completed()
            => Update(NumberOfValues);

        public ProgressInfo Update(int currentValue, int numberOfValues)
        {
            NumberOfValues = numberOfValues;
            return Update(currentValue);
        }

        public ProgressInfo Update(int currentValue)
        {
            CurrentValue = currentValue;
            Progress = (float)CurrentValue / NumberOfValues;
            Elapsed = DateTimeOffset.UtcNow - StartTimeUtc;
            return this;
        }

        public static ProgressInfo Create(Phase phase, int numberOfValues = 0, int currentValue = 0)
        {
            return new ProgressInfo
            {
                CurrentPhase = phase,
                NumberOfValues = numberOfValues,
                CurrentValue = currentValue,
                StartTimeUtc = DateTimeOffset.UtcNow,
            };
        }
    }
}
