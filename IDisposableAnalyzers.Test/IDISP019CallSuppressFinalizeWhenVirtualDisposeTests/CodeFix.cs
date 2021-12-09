namespace IDisposableAnalyzers.Test.IDISP019CallSuppressFinalizeWhenVirtualDisposeTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DisposeMethodAnalyzer Analyzer = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP019CallSuppressFinalizeVirtual);
        private static readonly SuppressFinalizeFix Fix = new();

        [Test]
        public static void WhenStatementBody()
        {
            var before = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private bool isDisposed = false;

        public void ↓Dispose()
        {
            this.Dispose(true);
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

        [Test]
        public static void WhenStatementBodyAndTrivia()
        {
            var before = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private bool isDisposed = false;

        public void ↓Dispose()
        {
            // 1
            this.Dispose(true);
            // 2
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

            var after = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private bool isDisposed = false;

        public void Dispose()
        {
            // 1
            this.Dispose(true);
            GC.SuppressFinalize(this);
            // 2
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

        [Test]
        public static void SealedWithFinalizerWhenExpressionBody()
        {
            var before = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private bool isDisposed = false;

        public void ↓Dispose() => this.Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";

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
