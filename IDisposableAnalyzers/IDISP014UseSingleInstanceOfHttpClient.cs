namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP014UseSingleInstanceOfHttpClient
    {
        public const string DiagnosticId = "IDISP014";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Use a single instance of HttpClient.",
            messageFormat: "Use a single instance of HttpClient.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Use a single instance of HttpClient.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
