namespace Benchmarque.Runtime
{
    using System;
    using System.Diagnostics;

    public class DisplayResult :
        RunResult
    {
        public DisplayResult(RunResult self, RunResult best)
            : base(self)
        {
            DurationDifference = self.Duration - best.Duration;

            decimal bestPerIteration = Math.Round((decimal)best.Duration/best.Iterations, 8);
            decimal selfPerIteration = Math.Round((decimal)Duration/Iterations, 8);
            PercentageDifference = bestPerIteration != 0m
                                       ? Math.Round(selfPerIteration/bestPerIteration, 6)
                                       : 0m;
        }

        public long DurationDifference { get; set; }

        public decimal PercentageDifference { get; set; }

        public decimal DurationPerIteration
        {
            get { return Math.Round((decimal)Duration/Iterations, 4); }
        }

        public TimeSpan TimeDifference
        {
            get { return TimeSpan.FromSeconds(DurationDifference*1d/Stopwatch.Frequency); }
        }

        public decimal Throughput
        {
            get
            {
                try
                {
                    return Stopwatch.Frequency/((decimal)Duration/Iterations);
                }
                catch (DivideByZeroException)
                {
                    return 0.0m;
                }
            }
        }
    }
}