namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP023ReferenceTypeInFinalizerContext
    {
        public const string DiagnosticId = "IDISP023";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't use reference types in finalizer context.",
            messageFormat: "Don't use reference types in finalizer context.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't use reference types in finalizer context.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}