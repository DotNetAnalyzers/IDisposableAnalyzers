namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class SemanticModelCacheAnalyzer : Gu.Roslyn.AnalyzerExtensions.SyntaxTreeCacheAnalyzer
    {
    }
}
