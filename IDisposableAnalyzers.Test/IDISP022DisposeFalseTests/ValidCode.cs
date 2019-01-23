namespace IDisposableAnalyzers.Test.IDISP022DisposeFalseTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FinalizerAnalyzer();

        [Test]
        public void SealedWithFinalizer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
