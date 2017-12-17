namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        private static readonly string DisposableCode = @"
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
        public void PrivateReadonlyFieldInitializedWithFileOpenReadInDisposeMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void PrivateReadonlyFieldInitializedWithNewInDisposeMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
        }

        [Test]
        public void FieldAssignedInExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    class Foo : IDisposable
    {
        ↓IDisposable _disposable;
        public void Create() => _disposable = new Disposable();
        public void Dispose()
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    class Foo : IDisposable
    {
        IDisposable _disposable;
        public void Create() => _disposable = new Disposable();
        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
        }

        [Test]
        public void PrivateFieldThatCanBeNullInDisposeMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void FieldAssignedWithFileOpenReadInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream;

        public Foo()
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

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo()
        {
            this.stream = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void FieldAssignedWithNewDisposableInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public Foo()
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

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo()
        {
            this.disposable = new Disposable();
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
        }

        [Test]
        public void FieldWhenConditionallyAssignedInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream;

        public Foo(bool condition)
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

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo(bool condition)
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void FieldAssignedInCtorNullCoalescing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream;

        public Foo()
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

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo()
        {
            this.stream = null ?? File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void FieldAssignedInCtorTernary()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream;

        public Foo(bool value)
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

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;

        public Foo(bool value)
        {
            this.stream = value ? null : File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void ProtectedFieldInDisposeMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
    {
        protected Stream stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void FieldInDisposeMethod2()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void FieldInDisposeMethodExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void FieldOfTypeObjectInDisposeMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
    {
        private readonly object stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.stream as IDisposable)?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void PropertyWhenInitializedInline()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);

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

    public sealed class Foo : IDisposable
    {
        public Stream Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void GetOnlyPropertyWhenInitializedInline()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);

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

    public sealed class Foo : IDisposable
    {
        public Stream Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void GetSetPropertyOfTypeObjectWhenInitializedInline()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public object Stream { get; set; } = File.OpenRead(string.Empty);

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

    public sealed class Foo : IDisposable
    {
        public object Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.Stream as IDisposable)?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void GetOnlyPropertyOfTypeObjectWhenInitializedInline()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public object Stream { get; } = File.OpenRead(string.Empty);

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

    public sealed class Foo : IDisposable
    {
        public object Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.Stream as IDisposable)?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void GetSetPropertyWhenInitializedInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        ↓public Stream Stream { get; set; }

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

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; set; }

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void GetOnlyPropertyWhenInitializedInCtorVirtualDisposeUnderscoreNames()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo : IDisposable
    {
        private bool _disposed;

        public Foo()
        {
            Stream = File.OpenRead(string.Empty);
        }

        ↓public Stream Stream { get; }

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

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo : IDisposable
    {
        private bool _disposed;

        public Foo()
        {
            Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }

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
                Stream?.Dispose();
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void GetPrivateSetPropertyWithBackingFieldWhenInitializedInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private Stream _stream;

        public Foo()
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

    public sealed class Foo : IDisposable
    {
        private Stream _stream;

        public Foo()
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void GetOnlyPropertyWhenInitializedInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        ↓public Stream Stream { get; }

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

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void DisposeMemberWhenVirtualDisposeMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo : IDisposable
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

    public class Foo : IDisposable
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void DisposeMemberWhenVirtualDisposeMethodUnderscoreNames()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class Foo : IDisposable
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

    public abstract class Foo : IDisposable
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
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

    public class Foo : BaseClass
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
                this.stream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
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

    public class Foo : BaseClass
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
                this.stream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
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

    public class Foo : BaseClass
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

    public class Foo : BaseClass
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
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

    public class Foo : BaseClass
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

    public class Foo : BaseClass
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { baseCode, testCode }, fixedCode);
        }

        [Test]
        public void PrivateReadonlyFieldOfTypeSubclassInDisposeMethod()
        {
            var subclassCode = @"
namespace RoslynSandbox
{
    public sealed class Bar : Disposable
    {
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Bar bar = new Bar();

        public void Dispose()
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly Bar bar = new Bar();

        public void Dispose()
        {
            this.bar.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, subclassCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, subclassCode, testCode }, fixedCode);
        }

        [Test]
        public void PrivateReadonlyFieldOfTypeSubclassGenericInDisposeMethod()
        {
            var subclassCode = @"
namespace RoslynSandbox
{
    public sealed class Bar<T> : Disposable
    {
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo<T> : IDisposable
    {
        ↓private readonly Bar<T> bar = new Bar<T>();

        public void Dispose()
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo<T> : IDisposable
    {
        private readonly Bar<T> bar = new Bar<T>();

        public void Dispose()
        {
            this.bar.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, subclassCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, subclassCode, testCode }, fixedCode);
        }

        [Test]
        public void WhenNotCallingBaseDisposeWithBaseCode()
        {
            var fooBaseCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class FooBase : IDisposable
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
    public class Foo : FooBase
    {
        ↓protected override void Dispose(bool disposing)
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo : FooBase
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, fooBaseCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, fooBaseCode, testCode }, fixedCode);
        }

        [Test]
        public void WhenNotCallingBaseDisposeWithoutBaseCode()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : StreamReader
    {
        public Foo(Stream stream)
            : base(stream)
        {
        }

        ↓protected override void Dispose(bool disposing)
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
        public Foo(Stream stream)
            : base(stream)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
        }

        [Test]
        public void LazyPropertyBackingField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
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

    public sealed class Foo : IDisposable
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
        }

        [Test]
        public void AssignedInCoalesce()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        ↓private readonly IDisposable created;
        private bool disposed;

        public Foo(IDisposable injected)
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

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable created;
        private bool disposed;

        public Foo(IDisposable injected)
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
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
        }
    }
}