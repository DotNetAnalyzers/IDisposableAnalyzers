namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP013AwaitInUsing
    {
        public const string DiagnosticId = "IDISP013";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Await in using.",
            messageFormat: "Await in using.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Await in using.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
