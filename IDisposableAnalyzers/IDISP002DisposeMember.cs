namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class IDISP002DisposeMember
    {
        public const string DiagnosticId = "IDISP002";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Dispose member.",
            messageFormat: "Dispose member.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Dispose the member as it is assigned with a created IDisposable.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
