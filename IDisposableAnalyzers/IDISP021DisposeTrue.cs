namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP021DisposeTrue
    {
        public const string DiagnosticId = "IDISP021";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Call this.Dispose(true).",
            messageFormat: "Call this.Dispose(true).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call this.Dispose(true).",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
