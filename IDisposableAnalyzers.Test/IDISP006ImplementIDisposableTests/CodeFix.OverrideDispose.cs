namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        public class OverrideDispose
        {
            [Test]
            public void SubclassStreamReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : StreamReader
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo(string path)
            : base(path)
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : StreamReader
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public Foo(string path)
            : base(path)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }

        protected virtual void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
                AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
            }

            [Test]
            public void StyleCopCallingBaseThrowIfDisposed()
            {
                var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class BaseClass : IDisposable
    {
        private bool disposed;

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
            }
        }

        protected virtual void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
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
            }

            base.Dispose(disposing);
        }

        protected override void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new System.ObjectDisposedException(this.GetType().FullName);
            }

            base.ThrowIfDisposed();
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
            }

            [Test]
            public void UnderscoreWhenThrowIsNotVirtual()
            {
                var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class BaseClass : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
    {
        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
    {
        private readonly Stream _stream = File.OpenRead(string.Empty);
        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
            }

            [Test]
            public void UnderscoreWhenThrowIsVirtual()
            {
                var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class BaseClass : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
            }
        }

        protected virtual void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
    {
        ↓private readonly Stream _stream = File.OpenRead(string.Empty);
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : BaseClass
    {
        private readonly Stream _stream = File.OpenRead(string.Empty);
        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }

        protected override void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new System.ObjectDisposedException(GetType().FullName);
            }

            base.ThrowIfDisposed();
        }
    }
}";
                AnalyzerAssert.CodeFix<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
            }
        }
    }
}