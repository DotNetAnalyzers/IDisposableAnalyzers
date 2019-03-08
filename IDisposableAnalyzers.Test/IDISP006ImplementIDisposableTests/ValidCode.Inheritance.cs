namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public class Inheritance
        {
            [Test]
            public void WhenNotCallingBaseDispose()
            {
                var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class CBase : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.disposable.Dispose();
            }
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C : CBase
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
        }
    }
}";

                AnalyzerAssert.Valid(Analyzer, Descriptor, DisposableCode, baseCode, testCode);
            }

            [Test]
            public void WhenCallingBaseDisposeAfterIfDisposedReturn()
            {
                var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class CBase : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.disposable.Dispose();
            }
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C : CBase
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }
    }
}";

                AnalyzerAssert.Valid(Analyzer, Descriptor, DisposableCode, baseCode, testCode);
            }

            [Test]
            public void WhenCallingBaseDispose()
            {
                var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class CBase : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.disposable.Dispose();
            }
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C : CBase
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";

                AnalyzerAssert.Valid(Analyzer, Descriptor, DisposableCode, baseCode, testCode);
            }
        }
    }
}
