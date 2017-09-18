namespace IDisposableAnalyzers.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IDisposableAnalyzers.Benchmarks.Benchmarks;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class BenchmarkWalkerTests
    {
        private static readonly IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers = typeof(KnownSymbol).Assembly.GetTypes()
                               .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                               .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                               .ToArray();

        private static readonly IReadOnlyList<Type> AllBenchmarkTypes = typeof(AnalyzerBenchmarks).Assembly.GetTypes()
                                                                                                  .Where(typeof(AnalyzerBenchmarks).IsAssignableFrom)
                                                                                                  .ToArray();

        [TestCaseSource(nameof(AllAnalyzers))]
        public void Run(DiagnosticAnalyzer analyzer)
        {
            var walker = new BenchmarkWalker(Code.AnalyzersProject, analyzer);
            walker.Run();
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void AllAnalyzersHaveBenchmarks(DiagnosticAnalyzer analyzer)
        {
            var expectedName = analyzer.GetType().Name + "Benchmarks";
            var match = AllBenchmarkTypes.SingleOrDefault(x => x.Name == expectedName);
            Assert.NotNull(match, expectedName);
        }
    }
}