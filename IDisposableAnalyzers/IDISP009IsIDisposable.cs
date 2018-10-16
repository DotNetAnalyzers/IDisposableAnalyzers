namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP009IsIDisposable
    {
        public const string DiagnosticId = "IDISP009";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Add IDisposable interface.",
            messageFormat: "Add IDisposable interface.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The type has a Dispose method but does not implement IDisposable.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
