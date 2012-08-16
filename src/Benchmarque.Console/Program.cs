namespace Benchmarque.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Internals.Extensions;
    using Runtime;

    class Program
    {
        static void Main(string[] argv)
        {
            if (argv.Length == 0)
            {
                Console.WriteLine("usage: <assembly name> [filter]");
                return;
            }

            string subjectAssembly = argv[0];
            Console.WriteLine("Subject Assembly: {0}", subjectAssembly);

            string subjectPath = Path.GetFullPath(subjectAssembly);

            string privatePath = Path.GetDirectoryName(subjectPath);

            Assembly specs;
            try
            {
                IEnumerable<string> assembliesToLoad = GetDependentAssemblies(subjectPath);
                foreach (string loadAssemblyName in assembliesToLoad)
                {
                    string loadAssemblyPath = Path.Combine(privatePath, loadAssemblyName);

                    Assembly.LoadFrom(loadAssemblyPath);
                }

                Console.WriteLine("Loading subject assembly: {0}", subjectPath);
                specs = Assembly.LoadFrom(subjectPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load assembly: {0}", ex.Message);
                return;
            }

            string filter = null;
            if (argv.Length >= 2)
                filter = argv[1];

            Type[] specTypes = specs.GetTypes();

            var types = specTypes
                .Where(type => type.HasInterface(typeof(Benchmark<>)) && type.IsConcreteType())
                .Select(type => new
                    {
                        Type = type,
                        InputType = type.GetInterface(typeof(Benchmark<>)).GetGenericArguments()[0]
                    })
                .Where(type => filter == null || type.InputType.Name.Contains(filter))
                .OrderBy(x => x.InputType.Name);

            foreach (var benchmark in types)
            {
                Type[] subjectTypes = specTypes
                    .Where(type => type.HasInterface(benchmark.InputType))
                    .Where(type => type.IsConcreteType())
                    .OrderBy(x => x.Name)
                    .ToArray();

                Type runnerType = typeof(BenchmarkRunner<>).MakeGenericType(benchmark.InputType);
                var runner = (BenchmarkRunner)Activator.CreateInstance(runnerType, benchmark.Type, subjectTypes);

                IEnumerable<RunResult> results = runner.Run();

                Display(results);
            }
        }

        static void Display(IEnumerable<RunResult> results)
        {
            IEnumerable<IGrouping<int, RunResult>> groupBy = results.GroupBy(x => x.Iterations);

            foreach (var x in groupBy)
            {
                DisplayGroupResults(x);
            }
        }

        static void DisplayGroupResults(IGrouping<int, RunResult> group)
        {
            Console.WriteLine("Benchmark {0}, Runner {1}, {2} iterations", group.First().BenchmarkType.Name,
                group.First().RunnerType.Name, group.Key);

            Console.WriteLine();
            Console.WriteLine("{0,-30}{1,-14}{2,-12}{3,-10}{4,-12}{5,-12}{6}", "Implementation", "Duration",
                "Difference", "Each",
                "Multiplier", "Memory(KB)", "Throughput");
            Console.WriteLine(new string('=', 102));

            IOrderedEnumerable<RunResult> ordered = group.OrderBy(x => x.Duration);

            RunResult best = ordered.First();

            IEnumerable<DisplayResult> results = ordered.Select(x => new DisplayResult(x, best));
            foreach (DisplayResult x in results)
            {
                DisplayResult(group.Key, x);
            }

            Console.WriteLine();
        }

        static void DisplayResult(int count, DisplayResult result)
        {
            string testSubject = result.SubjectType.Name.Replace(result.RunnerType.Name, "");

            Console.WriteLine("{0,-30}{1,-14}{2,-12}{3,-10}{4,-12}{5,-12}{6}/s", testSubject,
                result.TimeDuration.ToFriendlyString(),
                result.TimeDifference.ToFriendlyString(),
                result.DurationPerIteration.ToString("F0"),
                result.PercentageDifference > 1m
                    ? result.PercentageDifference.ToString("F2") + "x"
                    : "",
                (result.MemoryUsage/1024).ToString("N0"),
                result.Throughput.ToString("N0"));
        }

        static IEnumerable<string> GetDependentAssemblies(string assemblyPath)
        {
            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);

            var graph = new DependencyGraph<string>();

            foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
            {
                string referencedName = Path.GetFileName(referencedAssembly.Name);

                AddDependentAssembly(graph, assemblyPath, referencedName);
            }

            string assemblyFileName = Path.GetFileName(assemblyPath);

            return graph.GetItemsInDependencyOrder(assemblyFileName)
                .Where(x => x != Path.GetFileName(assemblyPath))
                .Where(x => x != "Benchmarque.dll");
        }

        static void AddDependentAssembly(DependencyGraph<string> graph, string assemblyPath,
            string referencedAssemblyName)
        {
            string name = Path.GetFileName(assemblyPath);
            string path = Path.GetDirectoryName(assemblyPath);

            string referencedPath = Path.Combine(path, referencedAssemblyName + ".dll");
            if (File.Exists(referencedPath))
            {
                string assemblyName = Path.GetFileName(referencedPath);

                graph.Add(name, assemblyName);

                Assembly assembly = Assembly.ReflectionOnlyLoadFrom(referencedPath);
                foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
                {
                    string referencedName = Path.GetFileName(referencedAssembly.Name);

                    AddDependentAssembly(graph, assemblyName, referencedName);
                }
            }
        }
    }
}