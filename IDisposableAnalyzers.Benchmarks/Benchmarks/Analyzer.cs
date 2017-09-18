namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using BenchmarkDotNet.Attributes;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    public abstract class Analyzer
    {
        private readonly ProjectAndCompliation analyzersProject;
        private BenchmarkWalker walker;

        protected Analyzer(DiagnosticAnalyzer analyzer)
        {
            this.analyzersProject = new ProjectAndCompliation(
                Code.Solution.Projects.First(x => x.Name == "IDisposableAnalyzers.Analyzers"),
                analyzer);
            this.walker = new BenchmarkWalker(this.analyzersProject.Project, analyzer);
        }

        [Benchmark]
        public void RunOnIDisposableAnalyzersAnalyzers()
        {
            this.walker.Run();
            //return this.analyzersProject
            //           .CompilationWithAnalyzers()
            //           .GetAnalyzerDiagnosticsAsync(CancellationToken.None)
            //           .Result;
        }

        private class ProjectAndCompliation
        {
            private readonly DiagnosticAnalyzer analyzer;

            public ProjectAndCompliation(Project project, DiagnosticAnalyzer analyzer)
            {
                this.analyzer = analyzer;
                this.Project = project;
                this.Compilation = project
                    .GetCompilationAsync(CancellationToken.None)
                    .Result;
            }

            public Project Project { get; }

            public Compilation Compilation { get; }

            public CompilationWithAnalyzers CompilationWithAnalyzers()
            {
                return this.Compilation
                           .WithAnalyzers(
                               ImmutableArray.Create(this.analyzer),
                               this.Project.AnalyzerOptions,
                               CancellationToken.None);
            }
        }
    }
}
