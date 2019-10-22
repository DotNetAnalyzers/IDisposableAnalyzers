namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP007DontDisposeInjected
    {
        internal const string DiagnosticId = "IDISP007";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't dispose injected.",
            messageFormat: "Don't dispose injected.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't dispose disposables you do not own.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
