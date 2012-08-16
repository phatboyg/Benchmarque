namespace Benchmarque.Runtime
{
    using System;
    using System.Diagnostics;

    public class RunResult
    {
        public RunResult()
        {
        }

        protected RunResult(RunResult other)
        {
            Iterations = other.Iterations;
            Duration = other.Duration;
            Description = other.Description;
            SubjectType = other.SubjectType;
            BenchmarkType = other.BenchmarkType;
            RunnerType = other.RunnerType;
            MemoryUsage = other.MemoryUsage;
        }

        public int Iterations { get; set; }
        public long Duration { get; set; }

        public string Description { get; set; }

        public Type SubjectType { get; set; }

        public Type BenchmarkType { get; set; }

        public TimeSpan TimeDuration
        {
            get { return TimeSpan.FromSeconds(Duration * 1d / Stopwatch.Frequency); }
        }

        public Type RunnerType { get; set; }

        public long MemoryUsage { get; set; }
    }
}