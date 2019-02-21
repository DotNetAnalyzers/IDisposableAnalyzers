namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP004DontIgnoreCreated
    {
        public const string DiagnosticId = "IDISP004";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't ignore created IDisposable.",
            messageFormat: "Don't ignore created IDisposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't ignore created IDisposable.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
