namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP003DisposeBeforeReassigning
    {
        internal const string DiagnosticId = "IDISP003";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Dispose previous before re-assigning.",
            messageFormat: "Dispose previous before re-assigning.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Dispose previous before re-assigning.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
