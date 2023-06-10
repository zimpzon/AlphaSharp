using System;

namespace AlphaSharp
{
    public static class DefaultLogging
    {
        public static void Log(LogLevel logLevel, LogLevel maxLogLevel, bool logTimestamps, string msg)
        {
            if (logLevel > maxLogLevel)
                return;

            string timestamp = logTimestamps ? $"{DateTime.Now:HH:mm:ss} | " : "";
            string prefix = $"[{logLevel}] ";
            Console.WriteLine($"{timestamp}{prefix}{msg}");
        }

        public static void LogProgress(ProgressInfo progress, bool logTimestamps)
        {
            string timestamp = logTimestamps ? $"{DateTime.Now:HH:mm:ss} | " : "";
            Console.WriteLine($"{timestamp}[{progress.CurrentPhase}] {progress.Progress * 100:0.00}% ({progress.CurrentValue}/{progress.Count}) {progress.Elapsed}");
        }
    }
}
