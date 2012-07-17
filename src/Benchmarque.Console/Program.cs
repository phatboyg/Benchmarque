namespace Benchmarque.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
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

            Console.WriteLine("Current Working Directory: {0}", Directory.GetCurrentDirectory());

            string subjectAssembly = argv[0];
            Console.WriteLine("Subject Assembly: {0}", subjectAssembly);

            string subjectPath = Path.GetFullPath(subjectAssembly);
            Console.WriteLine("Subject path: {0}", subjectPath);

            string privatePath = Path.GetDirectoryName(subjectPath);
            Console.WriteLine("Private path: {0}", privatePath);

            subjectAssembly = Path.GetFileNameWithoutExtension(subjectPath);
            Console.WriteLine("Final Subject Assembly: {0}", subjectAssembly);

            SetupPrivateBinPath(privatePath);

            Assembly specs;
            try
            {
                IEnumerable<string> assembliesToLoad = GetDependentAssemblies(subjectPath);
                foreach (string loadAssemblyName in assembliesToLoad)
                {
                    string loadAssemblyPath = Path.Combine(privatePath, loadAssemblyName);
                    Console.WriteLine("Loading assembly: {0}", loadAssemblyPath);

                    Assembly.LoadFrom(loadAssemblyPath);
                }

                Console.WriteLine("Loading subject assembly: {0}", subjectPath);
                specs = Assembly.LoadFile(subjectPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load assembly: {0}", ex.Message);
                return;
            }

            string filter = null;
            if (argv.Length >= 2)
                filter = argv[1];

            var types = specs.GetTypes()
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
                Type[] subjectTypes = specs.GetTypes()
                    .Where(type => type.HasInterface(benchmark.InputType))
                    .Where(type => type.IsConcreteType())
                    .OrderBy(x => x.Name)
                    .ToArray();

                Type runnerType = typeof(BenchmarkRunner<>).MakeGenericType(benchmark.InputType);
                var runner = (BenchmarkRunner)Activator.CreateInstance(runnerType, benchmark.Type, subjectTypes);

                IEnumerable<RunResult> results = runner.Run();

                Display(results);
            }
            ;
        }

        static void SetupPrivateBinPath(string path)
        {
            var current = AppDomain.CurrentDomain.GetData("PRIVATE_BINPATH") as string;

            var appendPath = new StringBuilder();

            if (!string.IsNullOrEmpty(current))
            {
                // See if the last character is a separator 
                appendPath.Append(current);
                if ((current[current.Length - 1] != Path.PathSeparator) &&
                    (path[0] != Path.PathSeparator))
                    appendPath.Append(Path.PathSeparator);
            }
            appendPath.Append(path);

            string result = appendPath.ToString();

            AppDomain.CurrentDomain.SetData("PRIVATE_BINPATH", result);
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
            Console.WriteLine("{0,-30}{1,-14}{2,-12}{3,-10}{4}", "Implementation", "Duration", "Difference", "Each",
                "Multiplier");
            Console.WriteLine(new string('=', 78));

            IOrderedEnumerable<RunResult> ordered = group.OrderBy(x => x.Duration);

            RunResult best = ordered.First();

            IEnumerable<DisplayResult> results = ordered.Select(x => new DisplayResult(x, best));
            foreach (DisplayResult x in results)
            {
                DisplayResult(x);
            }

            Console.WriteLine();
        }

        static void DisplayResult(DisplayResult result)
        {
            string testSubject = result.SubjectType.Name.Replace(result.RunnerType.Name, "");

            Console.WriteLine("{0,-30}{1,-14}{2,-12}{3,-10}{4}", testSubject,
                result.TimeDuration.ToFriendlyString(),
                result.TimeDifference.ToFriendlyString(),
                result.DurationPerIteration.ToString("F0"),
                result.PercentageDifference > 1m
                    ? result.PercentageDifference.ToString("F2") + "x"
                    : "");
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

            string name = Path.GetFileName(assemblyPath);

            return graph.GetItemsInDependencyOrder(name);
        }

        static void AddDependentAssembly(DependencyGraph<string> graph, string assemblyPath,
            string referencedAssemblyName)
        {
            string name = Path.GetFileName(assemblyPath);
            string path = Path.GetDirectoryName(assemblyPath);

            Console.WriteLine("Parsing dependency of {0}: {1}", name, referencedAssemblyName);

            string referencedPath = Path.Combine(path, referencedAssemblyName);
            if (File.Exists(referencedPath))
            {
                graph.Add(name, referencedAssemblyName);

                Assembly assembly = Assembly.ReflectionOnlyLoadFrom(referencedPath);
                foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
                {
                    string referencedName = Path.GetFileName(referencedAssembly.Name);

                    AddDependentAssembly(graph, referencedAssemblyName, referencedName);
                }
            }
        }
    }
}