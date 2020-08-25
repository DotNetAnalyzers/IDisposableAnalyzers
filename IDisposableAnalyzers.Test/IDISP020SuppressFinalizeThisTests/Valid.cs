namespace IDisposableAnalyzers.Test.IDISP020SuppressFinalizeThisTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeMethodAnalyzer();

        [Test]
        public static void SealedWithFinalizer()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Abstract()
        {
            var code = @"
namespace N
{
    using System;

    public abstract class C : IDisposable
    {
        public abstract void Dispose();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ProtectedAbstract()
        {
            var code = @"
namespace N
{
    using System;

    public abstract class C : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
