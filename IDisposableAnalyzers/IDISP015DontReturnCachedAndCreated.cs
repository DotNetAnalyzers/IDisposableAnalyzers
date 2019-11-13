namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP015DontReturnCachedAndCreated
    {
        internal const string DiagnosticId = "IDISP015";

        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: DiagnosticId,
            title: "Member should not return created and cached instance.",
            messageFormat: "Member should not return created and cached instance.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Member should not return created and cached instance.");
    }
}
