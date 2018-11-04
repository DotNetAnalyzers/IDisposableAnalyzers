namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class InterfaceOnlyVirtual
        {
            private static readonly CodeFixProvider Fix = new ImplementIDisposableCodeFixProvider();
            //// ReSharper disable once InconsistentNaming
            private static readonly ExpectedDiagnostic CS0535 = ExpectedDiagnostic.Create(nameof(CS0535));

            [Test]
            public void AbstractClass()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class Foo : IDisposable
    {
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class Foo : IDisposable
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
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Fix, CS0535, testCode, fixedCode);
                AnalyzerAssert.FixAll(Fix, CS0535, testCode, fixedCode);
            }

            [Test]
            public void AbstractClassWithFields()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class Foo : IDisposable
    {
        public const int Value1 = 1;
        private const int Value2 = 2;

        public static readonly int Value3;
        public static readonly int Value4;

        private readonly int value5;
        private int value6;

        public Foo()
        {
            value5 = Value2;
            value6 = Value2;
        }

        public int Bar => this.value5 + this.value6;
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class Foo : IDisposable
    {
        public const int Value1 = 1;
        private const int Value2 = 2;

        public static readonly int Value3;
        public static readonly int Value4;

        private readonly int value5;
        private int value6;
        private bool disposed;

        public Foo()
        {
            value5 = Value2;
            value6 = Value2;
        }

        public int Bar => this.value5 + this.value6;

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
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Fix, CS0535, testCode, fixedCode);
                AnalyzerAssert.FixAll(Fix, CS0535, testCode, fixedCode);
            }

            [Test]
            public void AbstractClassWithMethods()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class Foo : IDisposable
    {
        public void Bar1()
        {
        }

        internal void Bar2()
        {
        }

        protected void Bar3()
        {
        }

        private void Bar4()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class Foo : IDisposable
    {
        private bool disposed;

        public void Bar1()
        {
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        internal void Bar2()
        {
        }

        protected void Bar3()
        {
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
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private void Bar4()
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Fix, CS0535, testCode, fixedCode);
                AnalyzerAssert.FixAll(Fix, CS0535, testCode, fixedCode);
            }

            [Test]
            public void VirtualDispose()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
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
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Fix, CS0535, testCode, fixedCode, "Implement IDisposable with virtual dispose method.");
            }
        }
    }
}
