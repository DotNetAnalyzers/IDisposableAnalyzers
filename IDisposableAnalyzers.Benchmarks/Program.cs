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

    public class Program
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
                Cache<Microsoft.CodeAnalysis.SyntaxTree, Microsoft.CodeAnalysis.SemanticModel>.Begin();
                benchmark.Run();
                Cache<Microsoft.CodeAnalysis.SyntaxTree, Microsoft.CodeAnalysis.SemanticModel>.End();
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
                Cache<Microsoft.CodeAnalysis.SyntaxTree, Microsoft.CodeAnalysis.SemanticModel>.Begin();
                foreach (var summary in RunAll())
                {
                    CopyResult(summary);
                }

                Cache<Microsoft.CodeAnalysis.SyntaxTree, Microsoft.CodeAnalysis.SemanticModel>.End();
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
            yield return BenchmarkRunner.Run<T>();
        }

        private static void CopyResult(Summary summary)
        {
            var sourceFileName = Directory.EnumerateFiles(summary.ResultsDirectoryPath, $"*{summary.Title}-report-github.md")
                                          .Single();
            var destinationFileName = Path.ChangeExtension(FindCsFile(), ".md");
            Console.WriteLine($"Copy: {sourceFileName}");
            Console.WriteLine($"   -> {destinationFileName}");
            File.Copy(sourceFileName, destinationFileName, overwrite: true);

            string FindCsFile()
            {
                return Directory.EnumerateFiles(
                                    AppDomain.CurrentDomain.BaseDirectory.Split(new[] { "\\bin\\" }, StringSplitOptions.RemoveEmptyEntries).First(),
                                    $"{summary.Title.Split('.').Last()}.cs",
                                    SearchOption.AllDirectories)
                                .Single();
            }
        }
    }
}
