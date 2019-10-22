namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP019CallSuppressFinalizeWhenVirtualDispose
    {
        internal const string DiagnosticId = "IDISP019";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Call SuppressFinalize.",
            messageFormat: "Call SuppressFinalize(this).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call SuppressFinalize as there is a virtual dispose method.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
