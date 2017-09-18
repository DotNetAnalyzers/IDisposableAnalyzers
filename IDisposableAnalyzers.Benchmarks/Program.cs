// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
namespace IDisposableAnalyzers.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;
    using Gu.Roslyn.Asserts;
    using IDisposableAnalyzers.Benchmarks.Benchmarks;

    public class Program
    {
        public static string BenchmarksDirectory { get; } = Path.Combine(ProjectDirectory, "Benchmarks");

        public static string ProjectDirectory
        {
            get
            {
                var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(new Uri(typeof(Program).Assembly.CodeBase, UriKind.Absolute).LocalPath));
                if (CodeFactory.TryFindFileInParentDirectory(directoryInfo, "IDisposableAnalyzers.Benchmarks.csproj", out var projectfile))
                {
                    return projectfile.Directory.FullName;
                }

                throw new FileNotFoundException();
            }
        }

        private static string ArtifactsDirectory { get; } = Path.Combine(ProjectDirectory, "BenchmarkDotNet.Artifacts", "results");

        public static void Main()
        {
            if (true)
            {
                var walker = new BenchmarkWalker(Code.AnalyzersProject, new IDISP001DisposeCreated());
                walker.Run();
                Console.WriteLine("Attach profiler and press any key to continue...");
                Console.ReadKey();
                walker.Run();
            }
            else
            {
                foreach (var summary in RunAll())
                {
                    CopyResult(summary.Title);
                }
            }
        }

        private static IEnumerable<Summary> RunAll()
        {
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
            Console.WriteLine($"DestinationDirectory: {BenchmarksDirectory}");
            if (Directory.Exists(BenchmarksDirectory))
            {
                var sourceFileName = Path.Combine(ArtifactsDirectory, name + "-report-github.md");
                var destinationFileName = Path.Combine(BenchmarksDirectory, name + ".md");
                Console.WriteLine($"Copy: {sourceFileName} -> {destinationFileName}");
                File.Copy(sourceFileName, destinationFileName, overwrite: true);
            }
        }
    }
}
