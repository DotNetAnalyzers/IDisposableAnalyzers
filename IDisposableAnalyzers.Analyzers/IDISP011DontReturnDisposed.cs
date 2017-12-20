namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP011DontReturnDisposed
    {
        public const string DiagnosticId = "IDISP011";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't return diposed instance.",
            messageFormat: "Don't return diposed instance.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Don't return diposed instance.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}