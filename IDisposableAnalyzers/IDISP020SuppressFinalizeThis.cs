namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP020SuppressFinalizeThis
    {
        internal const string DiagnosticId = "IDISP020";

        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: DiagnosticId,
            title: "Call SuppressFinalize with this.",
            messageFormat: "Call SuppressFinalize(this).",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Call SuppressFinalize with this as argument.");
    }
}
