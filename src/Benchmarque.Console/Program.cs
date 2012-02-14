namespace Benchmarque.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Runtime;
    using Runtime.Internal;

    internal class Program
    {
        static void Main(string[] argv)
        {

            string loadPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine("Load Path: " + loadPath);


            SetupPrivateBinPath(loadPath);

            if (argv.Length == 0)
            {
                Console.WriteLine("usage: <assembly name> [filter]");
                return;
            }



            Assembly specs = Assembly.Load(argv[0]);

            string filter = null;
            if (argv.Length >= 2)
                filter = argv[1];

            var types = specs.GetTypes()
                .Where(type => type.HasInterface(typeof(Benchmark<>)) && type.IsConcreteType())
                .Select(type => new { Type = type, InputType = type.GetInterface(typeof(Benchmark<>)).GetGenericArguments()[0] })
                .Where(type => filter == null || type.InputType.Name.Contains(filter))
                .OrderBy(x => x.InputType.Name);

            foreach (var benchmark in types)
            {

                        Type[] subjectTypes = specs.GetTypes()
                            .Where(type => type.HasInterface(benchmark.InputType))
                            .Where(type => type.IsConcreteType())
                            .OrderBy(x => x.Name)
                            .ToArray();

                        var runner = (BenchmarkRunner)Activator.CreateInstance(typeof(BenchmarkRunner<>).MakeGenericType(benchmark.InputType),
                                                                           benchmark.Type, subjectTypes);

                        IEnumerable<RunResult> results = runner.Run();

                        Display(results);
                    };
        }

        static void SetupPrivateBinPath(string path)
        {
            string current = AppDomain.CurrentDomain.GetData("PRIVATE_BINPATH") as string;
            Console.WriteLine("Value: {0}", current);


            StringBuilder appendPath = new StringBuilder();

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

             current = AppDomain.CurrentDomain.GetData("PRIVATE_BINPATH") as string;

            Console.WriteLine("New Value: {0}", current);
        }

        static void Display(IEnumerable<RunResult> results)
        {
            var groupBy = results.GroupBy(x => x.Iterations)
              ;

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


            var results = ordered.Select(x => new DisplayResult(x, best));
            foreach (var x in results)
            {
                DisplayResult(x);
            }

            Console.WriteLine();
        }

        static void DisplayResult(DisplayResult result)
        {
            string testSubject = result.SubjectType.Name.Replace(result.RunnerType.Name, "");

            Console.WriteLine("{0,-30}{1,-14}{2,-12}{3,-10}{4}", testSubject, result.TimeDuration.ToFriendlyString(),
                              result.TimeDifference.ToFriendlyString(), result.DurationPerIteration,
                              result.PercentageDifference > 1m ? result.PercentageDifference.ToString("F2") + "x" : "");
        }
    }

}