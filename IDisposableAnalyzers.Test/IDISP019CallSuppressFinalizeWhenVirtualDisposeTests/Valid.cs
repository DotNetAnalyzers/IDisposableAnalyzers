namespace IDisposableAnalyzers.Test.IDISP019CallSuppressFinalizeWhenVirtualDisposeTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DisposeCallAnalyzer Analyzer = new();

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

        void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
