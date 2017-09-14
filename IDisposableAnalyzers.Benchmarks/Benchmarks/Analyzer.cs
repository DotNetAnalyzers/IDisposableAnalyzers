namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System.Collections.Immutable;
    using System.Threading;
    using BenchmarkDotNet.Attributes;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    public abstract class Analyzer
    {
        private readonly DiagnosticAnalyzer analyzer;
        private readonly Project project;
        private readonly Compilation compilation;

        protected Analyzer(DiagnosticAnalyzer analyzer)
        {
            this.analyzer = analyzer;
            this.project = Factory.CreateProject(analyzer);
            this.compilation = this.project
                                   .GetCompilationAsync(CancellationToken.None)
                                   .Result;
        }

        [Benchmark]
#pragma warning disable AvoidAsyncSuffix // Avoid Async suffix
        public object GetAnalyzerDiagnosticsAsync()
#pragma warning restore AvoidAsyncSuffix // Avoid Async suffix
        {
            return this.compilation.WithAnalyzers(
                           ImmutableArray.Create(this.analyzer),
                           this.project.AnalyzerOptions,
                           CancellationToken.None)
                       .GetAnalyzerDiagnosticsAsync(CancellationToken.None)
                       .Result;
        }
    }
}
