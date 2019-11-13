namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class Field
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly CodeFixProvider Fix = new DisposeMemberFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP002DisposeMember);

            [Test]
            public static void PrivateReadonlyInitializedWithFileOpenRead()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
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

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void PrivateReadonlyFieldInitializedWithNewDisposable()
            {
                var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable disposable = new Disposable();

        public void Dispose()
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
        private readonly IDisposable disposable = new Disposable();

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void AssignedInExpressionBody()
            {
                var before = @"
namespace N
{
    using System;

    class C : IDisposable
    {
        ↓IDisposable _disposable;

        public void M() => _disposable = new Disposable();

        public void Dispose()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    class C : IDisposable
    {
        IDisposable _disposable;

        public void M() => _disposable = new Disposable();

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void InitializedAndSetToNullInCtor()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private Stream stream = File.OpenRead(string.Empty);

        public void M()
        {
            this.stream.Dispose();
            this.stream = null;
        }

        public void Dispose()
        {
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
        private Stream stream = File.OpenRead(string.Empty);

        public void M()
        {
            this.stream.Dispose();
            this.stream = null;
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AssignedWithFileOpenReadInCtor()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly Stream stream;

        public C()
        {
            this.stream = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
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
        private readonly Stream stream;

        public C()
        {
            this.stream = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AssignedWithNewDisposableInCtor()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public C()
        {
            this.disposable = new Disposable();
        }

        public void Dispose()
        {
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
        private readonly IDisposable disposable;

        public C()
        {
            this.disposable = new Disposable();
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void ConditionallyAssignedInCtor()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly Stream stream;

        public C(bool condition)
        {
            if (condition)
            {
                this.stream = File.OpenRead(string.Empty);
            }
        }

        public void Dispose()
        {
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
        private readonly Stream stream;

        public C(bool condition)
        {
            if (condition)
            {
                this.stream = File.OpenRead(string.Empty);
            }
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AssignedInCtorNullCoalescing()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly Stream stream;

        public C()
        {
            this.stream = null ?? File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
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
        private readonly Stream stream;

        public C()
        {
            this.stream = null ?? File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AssignedInCtorTernary()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly Stream stream;

        public C(bool value)
        {
            this.stream = value ? null : File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
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
        private readonly Stream stream;

        public C(bool value)
        {
            this.stream = value ? null : File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void ProtectedSealedInitialized()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓protected Stream stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
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
        protected Stream stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void WhenAlreadyDisposingOther()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream1 = File.OpenRead(string.Empty);
        ↓private readonly Stream stream2 = File.OpenRead(string.Empty);

        public void Dispose()
        {
            stream1.Dispose();
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
        private readonly Stream stream1 = File.OpenRead(string.Empty);
        private readonly Stream stream2 = File.OpenRead(string.Empty);

        public void Dispose()
        {
            stream1.Dispose();
            this.stream2?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void DisposeMethodExpressionBody()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream1 = File.OpenRead(string.Empty);
        ↓private readonly Stream stream2 = File.OpenRead(string.Empty);

        public void Dispose() => this.stream1.Dispose();
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream1 = File.OpenRead(string.Empty);
        private readonly Stream stream2 = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.stream1.Dispose();
            this.stream2?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void OfTypeObject()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly object stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
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
        private readonly object stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.stream as IDisposable)?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void GetPrivateSetPropertyWithBackingFieldWhenInitializedInCtor()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private Stream _stream;

        public C()
        {
            Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return _stream; }
            private set { _stream = value; }
        }

        public void Dispose()
        {
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
        private Stream _stream;

        public C()
        {
            Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return _stream; }
            private set { _stream = value; }
        }

        public void Dispose()
        {
            _stream?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void DisposeMemberWhenVirtualDisposeMethodUnderscoreNames()
            {
                var before = @"
namespace N
{
    using System;

    public abstract class C : IDisposable
    {
        ↓private readonly IDisposable _disposable = new Disposable();

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

                var after = @"
namespace N
{
    using System;

    public abstract class C : IDisposable
    {
        private readonly IDisposable _disposable = new Disposable();

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
                _disposable.Dispose();
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void DisposeSecondMemberWhenOverriddenDisposeMethod()
            {
                var baseClass = @"
namespace N
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

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                var before = @"
namespace N
{
    using System.IO;

    public class C : BaseClass
    {
        private readonly Stream stream1 = File.OpenRead(string.Empty);
        ↓private readonly Stream stream2 = File.OpenRead(string.Empty);

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
                this.stream1.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;

    public class C : BaseClass
    {
        private readonly Stream stream1 = File.OpenRead(string.Empty);
        private readonly Stream stream2 = File.OpenRead(string.Empty);

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
                this.stream1.Dispose();
                this.stream2?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseClass, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseClass, before }, after);
            }

            [Test]
            public static void DisposeSecondMemberWhenOverriddenDisposeMethodNoCurlies()
            {
                var baseClass = @"
namespace N
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

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                var before = @"
namespace N
{
    using System.IO;

    public class C : BaseClass
    {
        private readonly Stream stream1 = File.OpenRead(string.Empty);
        ↓private readonly Stream stream2 = File.OpenRead(string.Empty);

        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
                this.stream1.Dispose();

            base.Dispose(disposing);
        }
    }
}";

                var after = @"
namespace N
{
    using System.IO;

    public class C : BaseClass
    {
        private readonly Stream stream1 = File.OpenRead(string.Empty);
        private readonly Stream stream2 = File.OpenRead(string.Empty);

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
                this.stream1.Dispose();
                this.stream2?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseClass, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseClass, before }, after);
            }

            [Test]
            public static void PrivateReadonlyFieldOfTypeSubclassInDisposeMethod()
            {
                var c1 = @"
namespace N
{
    using System;

    public sealed class C1 : Disposable
    {
    }
}";
                var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private readonly C1 c1 = new C1();

        public void Dispose()
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
        private readonly C1 c1 = new C1();

        public void Dispose()
        {
            this.c1.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, c1, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, c1, before }, after);
            }

            [Test]
            public static void PrivateReadonlyFieldOfTypeSubclassGenericInDisposeMethod()
            {
                var c1OfT = @"
namespace N
{
    using System;

    public sealed class C1<T> : Disposable
    {
    }
}";
                var before = @"
namespace N
{
    using System;

    public sealed class C<T> : IDisposable
    {
        ↓private readonly C1<T> c1 = new C1<T>();

        public void Dispose()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C<T> : IDisposable
    {
        private readonly C1<T> c1 = new C1<T>();

        public void Dispose()
        {
            this.c1.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, c1OfT, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, c1OfT, before }, after);
            }

            [Test]
            public static void LazyPropertyBackingField()
            {
                var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private IDisposable disposable;
        private bool disposed;

        public IDisposable Disposable => this.disposable ?? (this.disposable = new Disposable());

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

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private IDisposable disposable;
        private bool disposed;

        public IDisposable Disposable => this.disposable ?? (this.disposable = new Disposable());

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.disposable?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void AssignedInCoalesce()
            {
                var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable created;
        private bool disposed;

        public C(IDisposable injected)
        {
            this.Disposable = injected ?? (this.created = new Disposable());
        }

        public IDisposable Disposable { get; }

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

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable created;
        private bool disposed;

        public C(IDisposable injected)
        {
            this.Disposable = injected ?? (this.created = new Disposable());
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.created?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void AssignedInTernary()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private readonly Stream stream;

        public C()
        {
            var temp = File.OpenRead(string.Empty);
            this.stream = true
                ? temp
                : temp;
        }

        public void Dispose()
        {
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
        private readonly Stream stream;

        public C()
        {
            var temp = File.OpenRead(string.Empty);
            this.stream = true
                ? temp
                : temp;
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void DisposeMemberWhenVirtualDisposeMethod()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
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

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
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
                this.stream?.Dispose();
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void DisposeAfterIfNotDisposingReturn()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

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
            if (!disposing)
            {
                return;
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

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
            if (!disposing)
            {
                return;
            }

            this.stream?.Dispose();
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void DisposeFirstMemberWhenOverriddenDisposeMethod()
            {
                var baseClass = @"
namespace N
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

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                var before = @"
namespace N
{
    using System.IO;

    public class C : BaseClass
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
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
    }
}";

                var after = @"
namespace N
{
    using System.IO;

    public class C : BaseClass
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
                this.stream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseClass, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseClass, before }, after);
            }

            [Test]
            public static void DisposeFirstMemberWhenOverriddenDisposeMethodEmptyBlock()
            {
                var baseClass = @"
namespace N
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

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";
                var before = @"
namespace N
{
    using System.IO;

    public class C : BaseClass
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
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
    }
}";

                var after = @"
namespace N
{
    using System.IO;

    public class C : BaseClass
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
                this.stream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseClass, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseClass, before }, after);
            }

            [Test]
            public static void CreateIfDisposingWhenEmpty()
            {
                var before = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        ↓private readonly IDisposable disposable = new Disposable();

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.disposable.Dispose();
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void WhenCallingBaseDispose()
            {
                var baseClass = @"
namespace N
{
    using System;

    public abstract class BaseClass : IDisposable
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
                var before = @"
namespace N
{
    using System;

    public class C : BaseClass
    {
        ↓private readonly IDisposable disposable = new Disposable();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C : BaseClass
    {
        private readonly IDisposable disposable = new Disposable();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.disposable.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, baseClass, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, baseClass, before }, after);
            }
        }
    }
}
