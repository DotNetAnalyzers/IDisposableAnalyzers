namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class ValidCode
    {
        public static class Dispose
        {
            private static readonly DiagnosticAnalyzer Analyzer = new DisposeMethodAnalyzer();

            private const string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

            [Test]
            public static void TouchingReferenceTypeInIfBlock()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Text;

    public sealed class C : IDisposable
    {
        private static readonly StringBuilder Builder = new StringBuilder();

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
                if (disposing)
                {
                    Builder.Append(1);
                }
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void TouchingReferenceTypeInIfExpression()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Text;

    public sealed class C : IDisposable
    {
        private static readonly StringBuilder Builder = new StringBuilder();

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
                if (disposing)
                    Builder.Append(1);
            }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [TestCase("isDisposed.Equals(false)")]
            [TestCase("isDisposed.Equals(this)")]
            public static void TouchingStruct(string expression)
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
                if (disposing)
                {
                }

                _ = isDisposed.Equals(false);
            }
        }
    }
}".AssertReplace("isDisposed.Equals(false)", expression);
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void SettingStaticToNull()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Text;

    public class C : IDisposable
    {
        private static StringBuilder Builder = new StringBuilder();
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
                if (disposing)
                {
                }

                Builder = null;
            }
        }
    }
}";

                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void SettingInstanceToNull()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Text;

    public class C : IDisposable
    {
        private StringBuilder Builder = new StringBuilder();
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
                if (disposing)
                {
                }

                this.Builder = null;
            }
        }
    }
}";

                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void WhenCallingBaseDispose()
            {
                var fooBaseCode = @"
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
            GC.SuppressFinalize(this);
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
    public class C : CBase
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";

                RoslynAssert.Valid(Analyzer, DisposableCode, fooBaseCode, testCode);
            }

            [Test]
            public static void WhenCallingBaseDisposeAfterCheckDispose()
            {
                var fooBaseCode = @"
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
            GC.SuppressFinalize(this);
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
    public class C : CBase
    {
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

                RoslynAssert.Valid(Analyzer, DisposableCode, fooBaseCode, testCode);
            }

            [Test]
            public static void WhenCallingBaseDisposeAfterCheckDisposeAndIfDisposing()
            {
                var fooBaseCode = @"
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
            GC.SuppressFinalize(this);
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
            if (disposing)
            {
                this.disposable.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";

                RoslynAssert.Valid(Analyzer, DisposableCode, fooBaseCode, testCode);
            }
        }
    }
}
