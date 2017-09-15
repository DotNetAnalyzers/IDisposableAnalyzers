namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;
    using NUnit.Framework;

    public class RunOnIDisposableAnalyzers
    {
        private static readonly ImmutableArray<DiagnosticAnalyzer> AllAnalyzers = typeof(KnownSymbol).Assembly.GetTypes()
                                                                                                     .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                                                                                                     .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                                                                                                     .ToImmutableArray();

        private static readonly Project Project = Factory.CreateProject(AllAnalyzers);

        public RunOnIDisposableAnalyzers()
        {
            // A warmup so that the timings for the tests are more relevant.
            var compilation = Project.GetCompilationAsync(CancellationToken.None)
                                     .Result
                                     .WithAnalyzers(
                                         ImmutableArray.Create<DiagnosticAnalyzer>(new IDISP001DisposeCreated()),
                                         Project.AnalyzerOptions,
                                         CancellationToken.None);
            compilation.GetAnalyzerDiagnosticsAsync(CancellationToken.None).Wait();
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public async Task GetAnalyzerDiagnosticsAsync(DiagnosticAnalyzer analyzer)
        {
            var compilation = Project.GetCompilationAsync(CancellationToken.None)
                                     .Result
                                     .WithAnalyzers(
                                         ImmutableArray.Create(analyzer),
                                         Project.AnalyzerOptions,
                                         CancellationToken.None);
            var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync(CancellationToken.None)
                                               .ConfigureAwait(false);
            if (diagnostics.Length == 0)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Inconclusive(string.Join(Environment.NewLine, diagnostics.Select(x => x.GetMessage())));
            }
        }

        private static class Factory
        {
            [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
            [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
            internal static Project CreateProject(IEnumerable<DiagnosticAnalyzer> analyzers)
            {
                var projFile = ProjFile(typeof(KnownSymbol)).FullName;
                var projectName = Path.GetFileNameWithoutExtension(projFile);
                var projectId = ProjectId.CreateNewId(projectName);
                var solution = CreateSolution(projectId, projectName);
                var doc = XDocument.Parse(File.ReadAllText(projFile));
                var directory = Path.GetDirectoryName(projFile);
                var compiles = doc.Descendants(XName.Get("Compile", "http://schemas.microsoft.com/developer/msbuild/2003"))
                                  .ToArray();
                if (compiles.Length == 0)
                {
                    throw new InvalidOperationException("Parsing failed, no <Compile ... /> found.");
                }

                foreach (var compile in compiles)
                {
                    var csFile = Path.Combine(directory, compile.Attribute("Include").Value);
                    var documentId = DocumentId.CreateNewId(projectId);
                    using (var stream = File.OpenRead(csFile))
                    {
                        solution = solution.AddDocument(documentId, csFile, SourceText.From(stream));
                    }
                }

                var project = solution.GetProject(projectId);
                return ApplyCompilationOptions(project, analyzers);
            }

            [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
            private static FileInfo ProjFile(Type typeInAssembly)
            {
                var directoryInfo = new FileInfo(new Uri(typeInAssembly.Assembly.CodeBase).LocalPath)
                    .Directory
                    .Parent
                    .Parent
                    .Parent
                    .Parent;
                return directoryInfo
                    .EnumerateFiles("IDisposableAnalyzers.Analyzers.csproj", SearchOption.AllDirectories)
                    .Single();
            }

            private static Solution CreateSolution(ProjectId projectId, string projectName)
            {
                var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
                var solution = new AdhocWorkspace()
                    .CurrentSolution
                    .AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
                    .WithProjectCompilationOptions(projectId, compilationOptions)
                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location).WithAliases(ImmutableArray.Create("global", "corlib")))
                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).Assembly.Location).WithAliases(ImmutableArray.Create("global", "system")))
                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(WebClient).Assembly.Location))
                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.Xml.Serialization.XmlSerializer).Assembly.Location))
                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location))
                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location));
                var parseOptions = solution.GetProject(projectId).ParseOptions;
                return solution.WithProjectParseOptions(projectId, parseOptions.WithDocumentationMode(DocumentationMode.Diagnose));
            }

            private static Project ApplyCompilationOptions(Project project, IEnumerable<DiagnosticAnalyzer> analyzer)
            {
                // update the project compilation options
                var diagnostics = ImmutableDictionary.CreateRange(
                    analyzer.SelectMany(
                        a => a.SupportedDiagnostics.Select(
                            x => new KeyValuePair<string, ReportDiagnostic>(x.Id, ReportDiagnostic.Warn))));

                var modifiedSpecificDiagnosticOptions = diagnostics.SetItems(project.CompilationOptions.SpecificDiagnosticOptions);
                var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);

                var solution = project.Solution.WithProjectCompilationOptions(project.Id, modifiedCompilationOptions);
                return solution.GetProject(project.Id);
            }
        }
    }
}