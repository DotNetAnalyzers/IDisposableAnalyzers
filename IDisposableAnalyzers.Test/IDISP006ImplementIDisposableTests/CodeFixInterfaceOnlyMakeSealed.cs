namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class InterfaceOnlyMakeSealed
        {
            private static readonly CodeFixProvider Fix = new ImplementIDisposableFix();
            //// ReSharper disable once InconsistentNaming
            private static readonly ExpectedDiagnostic CS0535 = ExpectedDiagnostic.Create(nameof(CS0535));

            [Test]
            public static void EmptyClass()
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

    public sealed class C : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
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
                RoslynAssert.CodeFix(Fix, CS0535, before, after, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public static void EmptyClassFigureOutUnderscoreFromOtherClass()
            {
                var c1 = @"
namespace N
{
    public class C1
    {
        private int _value;

        public C1(int value)
        {
            _value = value;
        }
    }
}";

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

    public sealed class C : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                RoslynAssert.CodeFix(Fix, CS0535, new[] { c1, before }, after, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public static void WithThrowIfDisposed()
            {
                var before = @"
namespace N
{
    using System;

    public class C : ↓IDisposable
    {
        private void ThrowIfDisposed()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void ThrowIfDisposed()
        {
        }
    }
}";
                RoslynAssert.CodeFix(Fix, CS0535, before, after, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public static void WithProtectedPrivateSetProperty()
            {
                var before = @"
namespace N
{
    using System;

    public class C : ↓IDisposable
    {
        protected int Value { get; private set; }
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool disposed;

        private int Value { get; set; }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
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
                RoslynAssert.CodeFix(Fix, CS0535, before, after, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public static void WithOverridingProperties()
            {
                var baseClass = @"
namespace N
{
    public abstract class BaseClass
    {
        public virtual int P1 { get; protected set; }

        public abstract int P2 { get; set; }

        protected virtual int P3 { get; set; }
    }
}";

                var before = @"
namespace N
{
    using System;

    public class C : BaseClass, ↓IDisposable
    {
        public override int P1 { get; protected set; }

        public override int P2 { get; set; }

        protected override int P3 { get; set; }
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C : BaseClass, IDisposable
    {
        private bool disposed;

        public override int P1 { get; protected set; }

        public override int P2 { get; set; }

        protected override int P3 { get; set; }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
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
                RoslynAssert.CodeFix(Fix, CS0535, new[] { baseClass, before }, after, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public static void WithPublicVirtualMethod()
            {
                var before = @"
namespace N
{
    using System;

    public class C : ↓IDisposable
    {
        public virtual void M()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool disposed;

        public void M()
        {
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
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
                RoslynAssert.CodeFix(Fix, CS0535, before, after, "Implement IDisposable and make class sealed.");
            }

            [Test]
            public static void WithProtectedVirtualMethod()
            {
                var before = @"
namespace N
{
    using System;

    public class C : ↓IDisposable
    {
        protected virtual void M()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void M()
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
                RoslynAssert.CodeFix(Fix, CS0535, before, after, "Implement IDisposable and make class sealed.");
            }
        }
    }
}
