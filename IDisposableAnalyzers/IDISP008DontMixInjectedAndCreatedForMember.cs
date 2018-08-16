namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP008DontMixInjectedAndCreatedForMember
    {
        public const string DiagnosticId = "IDISP008";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't assign member with injected and created disposables.",
            messageFormat: "Don't assign member with injected and created disposables.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't assign member with injected and created disposables. It creates a confusing ownership situation.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
