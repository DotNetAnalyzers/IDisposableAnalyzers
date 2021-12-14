namespace IDisposableAnalyzers.Test.IDISP009IsIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DisposeMethodAnalyzer Analyzer = new();
        private static readonly ImplementIDisposableFix Fix = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP009IsIDisposable);

        [Test]
        public static void AddInterfaceSimple()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public void ↓Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

            var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
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

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AddInterface()
        {
            var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public C()
        {
        }

        public int Value { get; }

        public void ↓Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void M1()
        {
        }

        private void M2()
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

            var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public C()
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

        private void M1()
        {
        }

        private void M2()
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

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
