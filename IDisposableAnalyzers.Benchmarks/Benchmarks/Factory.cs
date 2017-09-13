namespace IDisposableAnalyzers.Benchmarks.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Xml.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    internal static class Factory
    {
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        internal static Project CreateProject(DiagnosticAnalyzer analyzer)
        {
            var projFile = ProjFile(analyzer.GetType()).FullName; 
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
            return ApplyCompilationOptions(project, analyzer);
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static FileInfo ProjFile(Type typeInAssembly)
        {
            return new FileInfo(new Uri(typeInAssembly.Assembly.CodeBase).LocalPath)
                .Directory
                .Parent
                .Parent
                .Parent
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

        private static Project ApplyCompilationOptions(Project project, DiagnosticAnalyzer analyzer)
        {
            // update the project compilation options
            var diagnostics = ImmutableDictionary.CreateRange(
                analyzer.SupportedDiagnostics.Select(x => new KeyValuePair<string, ReportDiagnostic>(x.Id, ReportDiagnostic.Warn)));

            var modifiedSpecificDiagnosticOptions = diagnostics.SetItems(project.CompilationOptions.SpecificDiagnosticOptions);
            var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);

            var solution = project.Solution.WithProjectCompilationOptions(project.Id, modifiedCompilationOptions);
            return solution.GetProject(project.Id);
        }
    }
}