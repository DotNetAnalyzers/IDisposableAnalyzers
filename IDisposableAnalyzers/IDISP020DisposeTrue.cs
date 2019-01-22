namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP020DisposeTrue
    {
        public const string DiagnosticId = "IDISP020";

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
