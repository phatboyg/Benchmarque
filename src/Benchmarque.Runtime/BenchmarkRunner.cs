namespace Benchmarque.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public interface BenchmarkRunner
    {
        IEnumerable<RunResult> Run();
    }


    public class BenchmarkRunner<T> :
        BenchmarkRunner
        where T : class
    {
        readonly Type _benchmarkType;
        readonly Type[] _subjectTypes;
        readonly Benchmark<T> _benchmark;
        int _runCount = 3;

        public BenchmarkRunner(Type benchmarkType, Type[] subjectTypes)
        {
            _benchmarkType = benchmarkType;
            _subjectTypes = subjectTypes;

            _benchmark = (Benchmark<T>) Activator.CreateInstance(_benchmarkType);
        }

        public IEnumerable<RunResult> Run()
        {
            foreach (int loopCount in _benchmark.Iterations)
            {
                foreach (Type subjectType in _subjectTypes)
                {
                    IEnumerable<RunResult> metrics = RunMeasurement(subjectType, loopCount);

                    RunResult best = metrics.OrderByDescending(x => x.Duration)
                        .First();

                    yield return best;
                }
            }
        }


        IEnumerable<RunResult> RunMeasurement(Type subjectType, int loopCount)
        {
            var metrics = new List<RunResult>();

            for (int i = 0; i < _runCount; i++)
            {
                GC.Collect();
                var subject = (T) Activator.CreateInstance(subjectType);

                long duration = Measure(subject, loopCount);

                if (subject is IDisposable)
                    ((IDisposable) subject).Dispose();

                metrics.Add(new RunResult
                    {
                        Description = subjectType.Name,
                        BenchmarkType = _benchmarkType,
                        RunnerType = typeof (T),
                        SubjectType = subjectType,
                        Iterations = loopCount,
                        Duration = duration,
                    });
            }

            return metrics;
        }

        long Measure(T subject, int iteration)
        {
            _benchmark.WarmUp(subject);

            GC.Collect();

            long begin = Stopwatch.GetTimestamp();

            _benchmark.Run(subject, iteration);

            long end = Stopwatch.GetTimestamp();

            _benchmark.Shutdown(subject);

            long ticksTaken = (end - begin);

            return ticksTaken;
        }
    }
}