namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP018CallSuppressFinalizeWhenFinalizer
    {
        internal const string DiagnosticId = "IDISP018";

        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: DiagnosticId,
            title: "Call SuppressFinalize.",
            messageFormat: "Call SuppressFinalize(this).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call SuppressFinalize(this) as the type has a finalizer.");
    }
}
