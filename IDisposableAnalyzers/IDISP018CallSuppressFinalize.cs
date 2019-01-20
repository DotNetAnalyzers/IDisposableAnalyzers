namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP018CallSuppressFinalize
    {
        public const string DiagnosticId = "IDISP018";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Call SuppressFinalize.",
            messageFormat: "Call SuppressFinalize.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call SuppressFinalize.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
