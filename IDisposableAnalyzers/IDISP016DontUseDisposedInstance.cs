namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP016DontUseDisposedInstance
    {
        internal const string DiagnosticId = "IDISP016";

        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: DiagnosticId,
            title: "Don't use disposed instance.",
            messageFormat: "Don't use disposed instance.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't use disposed instance.");
    }
}
