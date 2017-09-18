// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
namespace IDisposableAnalyzers.Benchmarks
{
    using System.Collections.Generic;
    using System.IO;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;

    public class Program
    {
        //// ReSharper disable PossibleNullReferenceException
        private static readonly string DestinationDirectory = Path.Combine(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Benchmarks");
        //// ReSharper restore PossibleNullReferenceException

        public static void Main()
        {
            foreach (var summary in RunSingle<Benchmarks.IDISP002DisposeMember>())
            {
                CopyResult(summary.Title);
            }
        }

        private static IEnumerable<Summary> RunAll()
        {
            ////ClearAllResults();
            var switcher = new BenchmarkSwitcher(typeof(Program).Assembly);
            var summaries = switcher.Run(new[] { "*" });
            return summaries;
        }

        private static IEnumerable<Summary> RunSingle<T>()
        {
            var summaries = new[] { BenchmarkRunner.Run<T>() };
            return summaries;
        }

        private static void CopyResult(string name)
        {
#if DEBUG
#else
            var sourceFileName = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results", name + "-report-github.md");
            Directory.CreateDirectory(DestinationDirectory);
            var destinationFileName = Path.Combine(DestinationDirectory, name + ".md");
            File.Copy(sourceFileName, destinationFileName, overwrite: true);
#endif
        }

        private static void ClearAllResults()
        {
            if (Directory.Exists(DestinationDirectory))
            {
                foreach (var resultFile in Directory.EnumerateFiles(DestinationDirectory, "*.md"))
                {
                    File.Delete(resultFile);
                }
            }
        }
    }
}
