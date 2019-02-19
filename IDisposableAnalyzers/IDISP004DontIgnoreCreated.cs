namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP004DontIgnoreCreated
    {
        public const string DiagnosticId = "IDISP004";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't ignore return value of type IDisposable.",
            messageFormat: "Don't ignore return value of type IDisposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't ignore return value of type IDisposable.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
