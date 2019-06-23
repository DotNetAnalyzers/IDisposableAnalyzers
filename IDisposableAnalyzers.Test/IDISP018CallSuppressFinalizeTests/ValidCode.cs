namespace IDisposableAnalyzers.Test.IDISP018CallSuppressFinalizeTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeCallAnalyzer();

        [Test]
        public void SealedSimple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SealedNoFinalizer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDisposed = false;

        public void Dispose()
        {
            this.Dispose(true);
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

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

        void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
