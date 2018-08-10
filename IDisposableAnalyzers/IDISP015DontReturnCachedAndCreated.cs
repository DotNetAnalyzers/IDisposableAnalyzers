namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP015DontReturnCachedAndCreated
    {
        public const string DiagnosticId = "IDISP015";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Member should not return created and cached instance.",
            messageFormat: "Member should not return created and cached instance.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Member should not return created and cached instance.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
