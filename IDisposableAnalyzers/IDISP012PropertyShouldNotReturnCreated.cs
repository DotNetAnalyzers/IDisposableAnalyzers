namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP012PropertyShouldNotReturnCreated
    {
        internal const string DiagnosticId = "IDISP012";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Property should not return created disposable.",
            messageFormat: "Property should not return created disposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Property should not return created disposable.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
