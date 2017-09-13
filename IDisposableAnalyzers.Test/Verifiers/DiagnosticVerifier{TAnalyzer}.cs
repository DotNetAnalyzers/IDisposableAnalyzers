namespace IDisposableAnalyzers.Test
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis.Diagnostics;

    public class DiagnosticVerifier<TAnalyzer> : DiagnosticVerifier
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        private static readonly TAnalyzer Analyzer = new TAnalyzer();

        internal override IEnumerable<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            yield return Analyzer;
        }
    }
}
