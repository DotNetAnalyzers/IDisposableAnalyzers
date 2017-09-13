namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Text;

    public static class CodeFactory
    {
        private const int DefaultIndentationSize = 4;
        private const int DefaultTabSize = 4;
        private const bool DefaultUseTabs = false;
        private static readonly string TestProjectName = "TestProject";

        /// <summary>
        /// Create a <see cref="Document"/> from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string.</param>
        /// <param name="analyzers">The analyzers to use.</param>
        /// <param name="disabledDiagnostics">The analyzers to suppress.</param>
        /// <returns>A <see cref="Document"/> created from the source string.</returns>
        public static Document CreateDocument(string source, IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<string> disabledDiagnostics = null)
        {
            return CreateProject(new[] { source }, analyzers, disabledDiagnostics).Documents.Single();
        }

        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a <see cref="Project"/> and return the
        /// documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings.</param>
        /// <param name="analyzers">The analyzers to use.</param>
        /// <param name="disabledDiagnostics">The analyzers to suppress.</param>
        /// <returns>A collection of <see cref="Document"/>s representing the sources.</returns>
        public static Document[] GetDocuments(string[] sources, IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<string> disabledDiagnostics = null)
        {
            var project = CreateProject(sources, analyzers, disabledDiagnostics);
            var documents = project.Documents.ToArray();

            if (sources.Length != documents.Length)
            {
                throw new SystemException("Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        /// <summary>
        /// Create a project using the input strings as sources.
        /// </summary>
        /// <remarks>
        /// <para>This method first creates a <see cref="Project"/> and then
        /// applies compilation options to the project by calling <see cref="ApplyCompilationOptions"/>.</para>
        /// </remarks>
        /// <param name="sources">Classes in the form of strings.</param>
        /// <param name="analyzers">The analyzers to use.</param>
        /// <param name="disabledDiagnostics">The analyzers to suppress.</param>
        /// <returns>A <see cref="Project"/> created out of the <see cref="Document"/>s created from the source
        /// strings.</returns>
        public static Project CreateProject(string[] sources, IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<string> disabledDiagnostics = null)
        {
            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);
            var solution = CreateSolution(projectId, LanguageNames.CSharp);

            var filenames = CodeReader.CreateFileNamesFromSources(sources, "cs");
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                var newFileName = filenames[i];
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
            }

            var project = solution.GetProject(projectId);
            return ApplyCompilationOptions(project, analyzers, disabledDiagnostics);
        }

        /// <summary>
        /// Creates a solution that will be used as parent for the sources that need to be checked.
        /// </summary>
        /// <param name="projectId">The project identifier to use.</param>
        /// <param name="language">The language for which the solution is being created.</param>
        /// <returns>The created solution.</returns>
        private static Solution CreateSolution(ProjectId projectId, string language)
        {
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .WithProjectCompilationOptions(projectId, compilationOptions)
                .AddMetadataReference(projectId, MetadataReferences.MsCorlib)
                .AddMetadataReference(projectId, MetadataReferences.System)
                .AddMetadataReference(projectId, MetadataReferences.SystemCore)
                .AddMetadataReference(projectId, MetadataReferences.SystemNet)
                .AddMetadataReference(projectId, MetadataReferences.SystemData)
                .AddMetadataReference(projectId, MetadataReferences.PresentationCore)
                .AddMetadataReference(projectId, MetadataReferences.PresentationFramework)
                .AddMetadataReference(projectId, MetadataReferences.WindowsBase)
                .AddMetadataReference(projectId, MetadataReferences.SystemXml)
                .AddMetadataReference(projectId, MetadataReferences.SystemXaml)
                .AddMetadataReference(projectId, MetadataReferences.SystemReactive)
                .AddMetadataReference(projectId, MetadataReferences.SystemReactiveInterfaces)
                .AddMetadataReference(projectId, MetadataReferences.SystemReactiveLinq)
                .AddMetadataReference(projectId, MetadataReferences.CSharpSymbols)
                .AddMetadataReference(projectId, MetadataReferences.CodeAnalysis)
                .AddMetadataReference(projectId, MetadataReferences.NUnit);

            solution.Workspace.Options =
                    solution.Workspace.Options
                    .WithChangedOption(FormattingOptions.IndentationSize, language, DefaultIndentationSize)
                    .WithChangedOption(FormattingOptions.TabSize, language, DefaultTabSize)
                    .WithChangedOption(FormattingOptions.UseTabs, language, DefaultUseTabs);

            var parseOptions = solution.GetProject(projectId).ParseOptions;
            return solution.WithProjectParseOptions(projectId, parseOptions.WithDocumentationMode(DocumentationMode.Diagnose));
        }

        /// <summary>
        /// Applies compilation options to a project.
        /// </summary>
        /// <remarks>
        /// <para>The default implementation configures the project by enabling all supported diagnostics of analyzers
        /// included in <see cref="analyzers"/> as well as <c>AD0001</c>. After configuring these
        /// diagnostics, any diagnostic IDs indicated in <see cref="disabledDiagnostics"/> are explictly supressed
        /// using <see cref="ReportDiagnostic.Suppress"/>.</para>
        /// </remarks>
        /// <param name="project">The project.</param>
        /// <param name="analyzers">The analyzers to use.</param>
        /// <param name="disabledDiagnostics">The analyzers to suppress.</param>
        /// <returns>The modified project.</returns>
        private static Project ApplyCompilationOptions(Project project, IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<string> disabledDiagnostics)
        {
            var supportedDiagnosticsSpecificOptions = new Dictionary<string, ReportDiagnostic>();
            foreach (var analyzer in analyzers)
            {
                foreach (var diagnostic in analyzer.SupportedDiagnostics)
                {
                    // make sure the analyzers we are testing are enabled
                    supportedDiagnosticsSpecificOptions[diagnostic.Id] = ReportDiagnostic.Default;
                }
            }

            // Report exceptions during the analysis process as errors
            supportedDiagnosticsSpecificOptions.Add("AD0001", ReportDiagnostic.Error);
            if (disabledDiagnostics != null)
            {
                foreach (var id in disabledDiagnostics)
                {
                    supportedDiagnosticsSpecificOptions[id] = ReportDiagnostic.Suppress;
                }
            }

            // update the project compilation options
            var modifiedSpecificDiagnosticOptions = supportedDiagnosticsSpecificOptions.ToImmutableDictionary().SetItems(project.CompilationOptions.SpecificDiagnosticOptions);
            var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);

            var solution = project.Solution.WithProjectCompilationOptions(project.Id, modifiedCompilationOptions);
            return solution.GetProject(project.Id);
        }
    }
}
