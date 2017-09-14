namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using System.Threading.Tasks;
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
        public void NotDisposingPrivateReadonlyFieldInitializedWithFileOpenReadInDisposeMethod()
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
        public void ArrayOfStreamsFieldInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream[] streams = new[] { File.OpenRead(string.Empty) };

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
        private readonly Stream[] streams = new[] { File.OpenRead(string.Empty) };

        public void Dispose()
        {
            foreach(var stream in this.streams)
            {
                stream.Dispose();
            }
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public void NotDisposingPrivateReadonlyFieldInitializedWithNewInDisposeMethod()
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
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, new[] { DisposableCode, fixedCode });
        }

        [Test]
        public void NotDisposingFieldAssignedInExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    class Foo : IDisposable
    {
        ↓IDisposable _disposable;
        public void Create()  => _disposable = new Disposable();
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
        public void Create()  => _disposable = new Disposable();
        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, new[] { DisposableCode, fixedCode });
        }

        [Test]
        public void NotDisposingPrivateFieldThatCanBeNullInDisposeMethod()
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
        public void NotDisposingFieldAssignedWithFileOpenReadInCtor()
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
        public void NotDisposingFieldAssignedWithNewDisposableInCtor()
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
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(new[] { DisposableCode, testCode }, new[] { DisposableCode, fixedCode });
        }

        [Test]
        public void NotDisposingFieldWhenConditionallyAssignedInCtor()
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
            if(condition)
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
            if(condition)
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
        public void NotDisposingFieldAssignedInCtorNullCoalescing()
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
        public void NotDisposingFieldAssignedInCtorTernary()
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
        public void NotDisposingProtectedFieldInDisposeMethod()
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
        public void NotDisposingFieldInDisposeMethod2()
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
        public void NotDisposingFieldInDisposeMethodExpressionBody()
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
        public void NotDisposingFieldOfTypeObjectInDisposeMethod()
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
        public void NotDisposingPropertyWhenInitializedInline()
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
        public void NotDisposingGetOnlyPropertyWhenInitializedInline()
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
        public void NotDisposingGetSetPropertyOfTypeObjectWhenInitializedInline()
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
        public void NotDisposingGetOnlyPropertyOfTypeObjectWhenInitializedInline()
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
        public void NotDisposingGetSetPropertyWhenInitializedInCtor()
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
        public void NotDisposingGetPrivateSetPropertyWithBackingFieldWhenInitializedInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓private Stream _stream;

    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream
    {
        get { return _stream; }
        private set { _stream = value; }
    }

    public void Dispose()
    {
    }
}";

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private Stream _stream;

    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
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
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public async Task NotDisposingGetOnlyPropertyWhenInitializedInCtor()
        {
            var testCode = @"
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
}";

            var fixedCode = @"
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
}";
            AnalyzerAssert.CodeFix<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
            AnalyzerAssert.FixAll<IDISP002DisposeMember, DisposeMemberCodeFixProvider>(testCode, fixedCode);
        }

        [Test]
        public async Task DisposeMemberWhenVirtualDisposeMethod()
        {
            var testCode = @"
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
}";
            var expected = this.CSharpDiagnostic(IDISP002DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
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
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposeMemberWhenVirtualDisposeMethodUnderscoreNames()
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
            var expected = this.CSharpDiagnostic(IDISP002DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, testCode }, expected)
                      .ConfigureAwait(false);

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
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, testCode }, new[] { DisposableCode, fixedCode })
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposeFirstMemberWhenOverriddenDisposeMethod()
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
            var expected = this.CSharpDiagnostic(IDISP002DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { baseCode, testCode }, expected)
                      .ConfigureAwait(false);

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
            await this.VerifyCSharpFixAsync(new[] { baseCode, testCode }, new[] { baseCode, fixedCode })
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposeSecondMemberWhenOverriddenDisposeMethod()
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
            var expected = this.CSharpDiagnostic(IDISP002DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { baseCode, testCode }, expected)
                      .ConfigureAwait(false);

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
            await this.VerifyCSharpFixAsync(new[] { baseCode, testCode }, new[] { baseCode, fixedCode })
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingPrivateReadonlyFieldOfTypeSubclassInDisposeMethod()
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
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, subclassCode, testCode }, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, subclassCode, testCode }, new[] { DisposableCode, subclassCode, fixedCode }).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingPrivateReadonlyFieldOfTypeSubclassGenericInDisposeMethod()
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
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, subclassCode, testCode }, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, subclassCode, testCode }, new[] { DisposableCode, subclassCode, fixedCode }).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenNotCallingBaseDisposeWithBaseCode()
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
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, fooBaseCode, testCode }, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, fooBaseCode, testCode }, new[] { DisposableCode, fooBaseCode, fixedCode }).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenNotCallingBaseDisposeWithoutBaseCode()
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
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, testCode }, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, testCode }, new[] { DisposableCode, fixedCode }).ConfigureAwait(false);
        }
    }
}