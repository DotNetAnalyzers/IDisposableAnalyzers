namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class InterfaceOnlyVirtual
        {
            private static readonly CodeFixProvider Fix = new ImplementIDisposableFix();
            //// ReSharper disable once InconsistentNaming
            private static readonly ExpectedDiagnostic CS0535 = ExpectedDiagnostic.Create(nameof(CS0535));

            [Test]
            public static void AbstractClass()
            {
                var before = @"
namespace N
{
    using System;

    public abstract class C : ↓IDisposable
    {
    }
}";

                var after = @"
namespace N
{
    using System;

    public abstract class C : IDisposable
    {
        private bool disposed;

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
                RoslynAssert.CodeFix(Fix, CS0535, before, after);
                RoslynAssert.FixAll(Fix, CS0535, before, after);
            }

            [Test]
            public static void AbstractClassWithFields()
            {
                var before = @"
namespace N
{
    using System;

    public abstract class C : ↓IDisposable
    {
        public const int Value1 = 1;
        private const int Value2 = 2;

        public static readonly int Value3;
        public static readonly int Value4;

        private readonly int value5;
        private int value6;

        public C()
        {
            value5 = Value2;
            value6 = Value2;
        }

        public int M => this.value5 + this.value6;
    }
}";

                var after = @"
namespace N
{
    using System;

    public abstract class C : IDisposable
    {
        public const int Value1 = 1;
        private const int Value2 = 2;

        public static readonly int Value3;
        public static readonly int Value4;

        private readonly int value5;
        private int value6;
        private bool disposed;

        public C()
        {
            value5 = Value2;
            value6 = Value2;
        }

        public int M => this.value5 + this.value6;

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
                RoslynAssert.CodeFix(Fix, CS0535, before, after);
                RoslynAssert.FixAll(Fix, CS0535, before, after);
            }

            [Test]
            public static void AbstractClassWithMethods()
            {
                var before = @"
namespace N
{
    using System;

    public abstract class C : ↓IDisposable
    {
        public void M1()
        {
        }

        internal void M2()
        {
        }

        protected void M3()
        {
        }

        private void M4()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public abstract class C : IDisposable
    {
        private bool disposed;

        public void M1()
        {
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void M2()
        {
        }

        protected void M3()
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

        private void M4()
        {
        }
    }
}";
                RoslynAssert.CodeFix(Fix, CS0535, before, after);
                RoslynAssert.FixAll(Fix, CS0535, before, after);
            }

            [Test]
            public static void VirtualDispose()
            {
                var before = @"
namespace N
{
    using System;

    public class C : ↓IDisposable
    {
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private bool disposed;

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
                RoslynAssert.CodeFix(Fix, CS0535, before, after, "Implement IDisposable with virtual dispose method.");
            }
        }
    }
}
