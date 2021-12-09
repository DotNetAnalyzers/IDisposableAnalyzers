namespace IDisposableAnalyzers.Test.IDISP020SuppressFinalizeThisTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DisposeMethodAnalyzer Analyzer = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP020SuppressFinalizeThis);
        private static readonly ArgumentFix Fix = new();

        [TestCase("null")]
        public static void WhenStatementBody(string expression)
        {
            var before = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private bool isDisposed = false;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(↓null);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}".AssertReplace("null", expression);

            var after = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private bool isDisposed = false;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
