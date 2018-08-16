namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP017PreferUsing
    {
        public const string DiagnosticId = "IDISP017";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Prefer using.",
            messageFormat: "Prefer using.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Prefer using.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
