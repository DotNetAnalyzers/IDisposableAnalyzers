namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP006ImplementIDisposable
    {
        public const string DiagnosticId = "IDISP006";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Implement IDisposable.",
            messageFormat: "Implement IDisposable.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The member is assigned with a created IDisposables within the type. Implement IDisposable and dispose it.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
