namespace IDisposableAnalyzers.Test
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    public class CodeFixVerifier<TAnalyzer, TCodeFix> : CodeFixVerifier
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        private static readonly TAnalyzer Analyzer = new TAnalyzer();
        private static readonly TCodeFix CodeFix = new TCodeFix();

        internal override IEnumerable<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            yield return Analyzer;
        }

        protected sealed override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return CodeFix;
        }
    }
}
