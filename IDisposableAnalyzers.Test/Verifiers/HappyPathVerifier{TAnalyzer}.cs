namespace IDisposableAnalyzers.Test
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    internal abstract class HappyPathVerifier<TAnalyzer> : IHappyPathVerifier
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        private static readonly DiagnosticVerifier<TAnalyzer> DiagnosticVerifier = new DiagnosticVerifier<TAnalyzer>();

        [Test]
        public void IdMatches()
        {
            foreach (var diagnosticAnalyzer in DiagnosticVerifier.GetCSharpDiagnosticAnalyzers())
            {
                StringAssert.StartsWith(diagnosticAnalyzer.SupportedDiagnostics.Single().Id, diagnosticAnalyzer.GetType().Name);
                StringAssert.Contains(diagnosticAnalyzer.GetType().Name, this.GetType().FullName, "Name of test class does not match analyzer name.");
            }
        }

        public async Task VerifyHappyPathAsync(params string[] testCode)
        {
            await DiagnosticVerifier.VerifyCSharpDiagnosticAsync(testCode, Test.DiagnosticVerifier.EmptyDiagnosticResults, CancellationToken.None).ConfigureAwait(false);
        }
    }
}