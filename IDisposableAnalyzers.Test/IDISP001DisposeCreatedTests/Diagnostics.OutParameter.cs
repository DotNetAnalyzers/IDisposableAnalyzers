namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class OutParameter
        {
            private static readonly DiagnosticAnalyzer Analyzer = new ArgumentAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP001DisposeCreated.Descriptor);

            [TestCase("out _")]
            [TestCase("out var temp")]
            [TestCase("out var _")]
            [TestCase("out FileStream temp")]
            [TestCase("out FileStream _")]
            public void Discarded(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class C
    {
        public static bool M(int i) => TryGet(i, ↓out _);

        private static bool TryGet(int i, out FileStream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}".AssertReplace("out _", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
