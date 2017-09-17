namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using BenchmarkDotNet.Attributes;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;

    public abstract class Analyzer
    {
        private static readonly IReadOnlyList<MetadataReference> MetadataReferences = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location).WithAliases(ImmutableArray.Create("global", "corlib")),
            MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).Assembly.Location).WithAliases(ImmutableArray.Create("global", "system")),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(WebClient).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Xml.Serialization.XmlSerializer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
        };

        private static readonly Solution Sln = CodeFactory.CreateSolution(SlnFile, new DiagnosticAnalyzer[0], MetadataReferences);

        private readonly ProjectAndCompliation analyzersProject;

        protected Analyzer(DiagnosticAnalyzer analyzer)
        {
            this.analyzersProject = new ProjectAndCompliation(
                Sln.Projects.First(x => x.Name == "IDisposableAnalyzers.Analyzers"),
                analyzer);
        }

        private static FileInfo SlnFile
        {
            get
            {
                if (CodeFactory.TryFindFileInParentDirectory(
                    new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase, UriKind.Absolute).LocalPath).Directory,
                    "IDisposableAnalyzers.sln",
                    out FileInfo solutionFile))
                {
                    return solutionFile;
                }

                throw new InvalidOperationException("Did not find sln file.");
            }
        }

        [Benchmark]
        public object RunOnIDisposableAnalyzersAnalyzers()
        {
            return this.analyzersProject
                       .CompilationWithAnalyzers()
                       .GetAnalyzerDiagnosticsAsync(CancellationToken.None)
                       .Result;
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
