namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP010CallBaseDispose
    {
        public const string DiagnosticId = "IDISP010";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Call base.Dispose(true)",
            messageFormat: "Call base.Dispose(true)",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Call base.Dispose(true)",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
