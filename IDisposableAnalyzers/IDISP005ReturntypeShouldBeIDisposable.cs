namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP005ReturntypeShouldBeIDisposable
    {
        public const string DiagnosticId = "IDISP005";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Return type should indicate that the value should be disposed.",
            messageFormat: "Return type should indicate that the value should be disposed.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Return type should indicate that the value should be disposed.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
