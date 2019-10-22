namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP022DisposeFalse
    {
        internal const string DiagnosticId = "IDISP022";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Call this.Dispose(false).",
            messageFormat: "Call this.Dispose(false).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call this.Dispose(false).",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
