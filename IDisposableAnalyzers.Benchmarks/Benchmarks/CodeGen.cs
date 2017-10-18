namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class CodeGen
    {
        private static IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers { get; } = typeof(KnownSymbol).Assembly
                                                                                                    .GetTypes()
                                                                                                    .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                                                                                                    .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                                                                                                    .ToArray();

        [TestCaseSource(nameof(AllAnalyzers))]
        public void AnalyzersBenchmark(DiagnosticAnalyzer analyzer)
        {
            var id = analyzer.SupportedDiagnostics.Single().Id;
            var expectedName = id + (id.Contains("_") ? "_" : string.Empty) + "Benchmarks";
            var fileName = Path.Combine(Program.BenchmarksDirectory, expectedName + ".cs");
            var code = new StringBuilder().AppendLine($"namespace {this.GetType().Namespace}")
                                          .AppendLine("{")
                                          .AppendLine($"    public class {expectedName}")
                                          .AppendLine("    {")
                                          .AppendLine($"        private static readonly Gu.Roslyn.Asserts.Benchmark Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new {analyzer.GetType().FullName}());")
                                          .AppendLine()
                                          .AppendLine("        [BenchmarkDotNet.Attributes.Benchmark]")
                                          .AppendLine("        public void RunOnIDisposableAnalyzers()")
                                          .AppendLine("        {")
                                          .AppendLine("            Benchmark.Run();")
                                          .AppendLine("        }")
                                          .AppendLine("    }")
                                          .AppendLine("}")
                                          .ToString();
            if (!File.Exists(fileName) ||
                File.ReadAllText(fileName) != code)
            {
                File.WriteAllText(fileName, code);
                Assert.Fail();
            }
        }

        [Test]
        public void AllBenchmarks()
        {
            var fileName = Path.Combine(Program.BenchmarksDirectory, "AllBenchmarks.cs");
            var builder = new StringBuilder();
            builder.AppendLine($"namespace {this.GetType().Namespace}")
                   .AppendLine("{")
                   .AppendLine("    public class AllBenchmarks")
                   .AppendLine("    {");
            foreach (var analyzer in AllAnalyzers)
            {
                builder.AppendLine(
                           $"        private static readonly Gu.Roslyn.Asserts.Benchmark {analyzer.SupportedDiagnostics[0].Id.Replace("_", string.Empty)} = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new {analyzer.GetType().FullName}());")
                       .AppendLine();
            }

            foreach (var analyzer in AllAnalyzers)
            {
                builder.AppendLine($"        [BenchmarkDotNet.Attributes.Benchmark]")
                       .AppendLine($"        public void {analyzer.GetType().Name}()")
                       .AppendLine("        {")
                       .AppendLine($"            {analyzer.SupportedDiagnostics[0].Id.Replace("_", string.Empty)}.Run();")
                       .AppendLine("        }");
                if (!ReferenceEquals(analyzer, AllAnalyzers.Last()))
                {
                    builder.AppendLine();
                }
            }

            builder.AppendLine("    }")
                   .AppendLine("}");

            if (!File.Exists(fileName) ||
                File.ReadAllText(fileName) != builder.ToString())
            {
                File.WriteAllText(fileName, builder.ToString());
                Assert.Fail();
            }
        }
    }
}