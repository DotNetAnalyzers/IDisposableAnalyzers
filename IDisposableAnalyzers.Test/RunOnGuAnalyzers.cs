namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class RunOnIDisposableAnalyzers
    {
        private static readonly ImmutableArray<DiagnosticAnalyzer> AllAnalyzers = typeof(KnownSymbol).Assembly.GetTypes()
                                                                                                     .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                                                                                                     .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                                                                                                     .ToImmutableArray();

        private static readonly IReadOnlyList<MetadataReference> MetadataReferences = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location).WithAliases(ImmutableArray.Create("global", "corlib")),
            MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).Assembly.Location).WithAliases(ImmutableArray.Create("global", "system")),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(WebClient).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Xml.Serialization.XmlSerializer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(NUnit.Framework.Assert).Assembly.Location),
        };

        private static readonly FileInfo SlnFile = GetSlnFile();

        private static FileInfo GetSlnFile()
        {
            if (Gu.Roslyn.Asserts.CodeFactory.TryFindFileInParentDirectory(
                new FileInfo(
                    new Uri(
                        Assembly.GetExecutingAssembly()
                                .CodeBase,
                        UriKind.Absolute).LocalPath).Directory,
                "IDisposableAnalyzers.sln",
                out FileInfo solutionFile))
            {
                return solutionFile;
            }

            throw new InvalidOperationException("Did not find sln file.");
        }

        private static readonly Solution Sln = Gu.Roslyn.Asserts.CodeFactory.CreateSolution(SlnFile, AllAnalyzers, MetadataReferences);

        public RunOnIDisposableAnalyzers()
        {
            // A warmup so that the timings for the tests are more relevant.
            foreach (var project in Sln.Projects)
            {
                var compilation = project.GetCompilationAsync(CancellationToken.None)
                                         .Result
                                         .WithAnalyzers(
                                             ImmutableArray.Create<DiagnosticAnalyzer>(new IDISP001DisposeCreated()),
                                             project.AnalyzerOptions,
                                             CancellationToken.None);
                compilation.GetAnalyzerDiagnosticsAsync(CancellationToken.None).Wait();
            }
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public async Task GetAnalyzerDiagnosticsAsync(DiagnosticAnalyzer analyzer)
        {
            foreach (var project in Sln.Projects)
            {
                var compilation = project.GetCompilationAsync(CancellationToken.None)
                                         .Result
                                         .WithAnalyzers(
                                             ImmutableArray.Create(analyzer),
                                             project.AnalyzerOptions,
                                             CancellationToken.None);
                compilation.GetAnalyzerDiagnosticsAsync(CancellationToken.None).Wait();
                var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync(CancellationToken.None)
                                                   .ConfigureAwait(false);
                if (diagnostics.Length == 0)
                {
                    continue;
                }

                Assert.Inconclusive(string.Join(Environment.NewLine, diagnostics.Select(x => x.GetMessage())));
            }
        }
    }
}