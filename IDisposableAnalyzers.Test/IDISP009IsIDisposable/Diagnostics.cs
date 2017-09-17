namespace IDisposableAnalyzers.Test.IDISP009IsIDisposable
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;
    using IDISP009IsIDisposable = IDisposableAnalyzers.IDISP009IsIDisposable;

    internal class CodeFix
    {
        [Test]
        public void AddInterfaceSimple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        ↓public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            AnalyzerAssert.CodeFix<IDISP009IsIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void AddInterface()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public Foo()
        {
        }

        public int Value { get; }

        ↓public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void Bar()
        {
        }

        private void Meh()
        {
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public Foo()
        {
        }

        public int Value { get; }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void Bar()
        {
        }

        private void Meh()
        {
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";

            AnalyzerAssert.CodeFix<IDISP009IsIDisposable, ImplementIDisposableCodeFixProvider>(testCode, fixedCode);
        }
    }
}