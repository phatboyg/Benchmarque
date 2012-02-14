namespace Benchmarque
{
    using System.Collections.Generic;

    /// <summary>
    /// A class that can be benchmarked by the console
    /// </summary>
    public interface Benchmark<T>
        where T : class 
    {
        /// <summary>
        /// A list of iteration counts to execute with each benchmark
        /// </summary>
        IEnumerable<int> Iterations { get; }

        /// <summary>
        /// Any single-run operations that should be performed to prepare for the benchmark
        /// </summary>
        /// <param name="instance">The instance being tested</param>
        void WarmUp(T instance);

        /// <summary>
        /// Any post-run operations that should be performed to clean up
        /// </summary>
        /// <param name="instance"></param>
        void Shutdown(T instance);

        /// <summary>
        /// Run the operation being benchmarked the specified number of iterations
        /// </summary>
        /// <param name="instance">The instance being tested</param>
        /// <param name="iterationCount">The number of iterations to execute</param>
        void Run(T instance, int iterationCount);
    }
}