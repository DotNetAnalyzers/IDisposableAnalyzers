namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP004DontIgnoreCreated
    {
        internal const string DiagnosticId = "IDISP004";

        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: DiagnosticId,
            title: "Don't ignore created IDisposable.",
            messageFormat: "Don't ignore created IDisposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't ignore created IDisposable.");
    }
}
