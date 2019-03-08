namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class Field
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly CodeFixProvider Fix = new DisposeMemberFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP002DisposeMember.Descriptor);

            [Test]
            public void PrivateReadonlyInitializedWithFileOpenRead()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void PrivateReadonlyFieldInitializedWithNewDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AssignedInExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void InitializedAndSetToNullInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓private Stream stream = File.OpenRead(string.Empty);

        public void Meh()
        {
            this.stream.Dispose();
            this.stream = null;
        }

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private Stream stream = File.OpenRead(string.Empty);

        public void Meh()
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AssignedWithFileOpenReadInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AssignedWithNewDisposableInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void ConditionallyAssignedInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AssignedInCtorNullCoalescing()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AssignedInCtorTernary()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void ProtectedSealedInitialized()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void WhenAlreadyDisposingOther()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void DisposeMethodExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void OfTypeObject()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void GetPrivateSetPropertyWithBackingFieldWhenInitializedInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void DisposeMemberWhenVirtualDisposeMethodUnderscoreNames()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void DisposeSecondMemberWhenOverriddenDisposeMethod()
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

        protected void ThrowIfDisposed()
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
            }

            [Test]
            public void DisposeSecondMemberWhenOverriddenDisposeMethodNoCurlies()
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

        protected void ThrowIfDisposed()
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
            }

            [Test]
            public void PrivateReadonlyFieldOfTypeSubclassInDisposeMethod()
            {
                var barCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class M : Disposable
    {
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private readonly M bar = new M();

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly M bar = new M();

        public void Dispose()
        {
            this.bar.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, barCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, barCode, testCode }, fixedCode);
            }

            [Test]
            public void PrivateReadonlyFieldOfTypeSubclassGenericInDisposeMethod()
            {
                var barCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class M<T> : Disposable
    {
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C<T> : IDisposable
    {
        ↓private readonly M<T> bar = new M<T>();

        public void Dispose()
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C<T> : IDisposable
    {
        private readonly M<T> bar = new M<T>();

        public void Dispose()
        {
            this.bar.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, barCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, barCode, testCode }, fixedCode);
            }

            [Test]
            public void LazyPropertyBackingField()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AssignedInCoalesce()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AssignedInTernary()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void DisposeMemberWhenVirtualDisposeMethod()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void DisposeFirstMemberWhenOverriddenDisposeMethod()
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

        protected void ThrowIfDisposed()
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
            }

            [Test]
            public void DisposeFirstMemberWhenOverriddenDisposeMethodEmptyBlock()
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

        protected void ThrowIfDisposed()
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { baseCode, testCode }, fixedCode);
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
        ↓private readonly IDisposable disposable = new Disposable();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class C : CBase
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, baseCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, baseCode, testCode }, fixedCode);
            }
        }
    }
}
