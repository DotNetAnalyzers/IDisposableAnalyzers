// ReSharper disable HeuristicUnreachableCode
#pragma warning disable GU0011 // Don't ignore the return value.
#pragma warning disable CS0162 // Unreachable code detected
namespace IDisposableAnalyzers.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;
    using Gu.Roslyn.AnalyzerExtensions;
    using IDisposableAnalyzers.Benchmarks.Benchmarks;
    using Microsoft.CodeAnalysis;

    public static class Program
    {
        public static void Main()
        {
            if (false)
            {
                var benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new CreationAnalyzer());

                // Warmup
                benchmark.Run();
                Console.WriteLine("Attach profiler and press any key to continue...");
                Console.ReadKey();
                using (SyntaxTreeCache<SemanticModel>.Begin(null))
                {
                    benchmark.Run();
                }
            }
            else if (false)
            {
                var benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new CreationAnalyzer());
                //// Warmup
                benchmark.Run();
                var sw = Stopwatch.StartNew();
                ////Cache<Microsoft.CodeAnalysis.SyntaxTree, Microsoft.CodeAnalysis.SemanticModel>.Begin();
                benchmark.Run();
                sw.Stop();
                ////Cache<Microsoft.CodeAnalysis.SyntaxTree, Microsoft.CodeAnalysis.SemanticModel>.End();
                Console.WriteLine($"Took: {sw.Elapsed.TotalMilliseconds:F3} ms");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            else
            {
                using (SyntaxTreeCache<SemanticModel>.Begin(null))
                {
                    foreach (var summary in RunAll())
                    {
                        CopyResult(summary);
                    }
                }
            }
        }

        private static IEnumerable<Summary> RunAll() => new BenchmarkSwitcher(typeof(Program).Assembly).RunAll();

#pragma warning disable IDE0051 // Remove unused private members
        private static IEnumerable<Summary> RunSingle<T>()
#pragma warning restore IDE0051 // Remove unused private members
        {
            yield return BenchmarkRunner.Run<T>();
        }

        private static void CopyResult(Summary summary)
        {
            var name = summary.Title.Split('.').LastOrDefault()?.Split('-').FirstOrDefault();
            if (name == null)
            {
                Console.WriteLine("Did not find name in: " + summary.Title);
                Console.WriteLine("Press any key to exit.");
                _ = Console.ReadKey();
                return;
            }

            var pattern = $"{summary.Title.Split('-').First()}-report-github.md";
            var sourceFileName = Directory.EnumerateFiles(summary.ResultsDirectoryPath, pattern)
                                          .SingleOrDefault();
            if (sourceFileName == null)
            {
                Console.WriteLine("Did not find a file matching the pattern: " + pattern);
                Console.WriteLine("Press any key to exit.");
                _ = Console.ReadKey();
                return;
            }

            var destinationFileName = Path.ChangeExtension(FindCsFile(), ".md");
            Console.WriteLine($"Copy:");
            Console.WriteLine($"Source: {sourceFileName}");
            Console.WriteLine($"Target: {destinationFileName}");
            File.Copy(sourceFileName, destinationFileName, overwrite: true);

            string FindCsFile()
            {
                return Directory.EnumerateFiles(
                                    AppDomain.CurrentDomain.BaseDirectory.Split(new[] { "\\bin\\" }, StringSplitOptions.RemoveEmptyEntries).First(),
                                    $"{name}.cs",
                                    SearchOption.AllDirectories)
                                .Single();
            }
        }
    }
}
