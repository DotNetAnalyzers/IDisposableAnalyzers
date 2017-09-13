namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<IDISP006ImplementIDisposable, ImplementIDisposableCodeFixProvider>
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
        public async Task ImplementIDisposable0()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
    }

    public int Value { get; }

    protected virtual void Bar()
    {
    }

    private void Meh()
    {
    }
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
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

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }
    }

    private void Meh()
    {
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposable1()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
    }

    public int Value { get; }

    public int this[int value]
    {
        get
        {
            return value;
        }
    }

    protected virtual void Bar()
    {
    }

    private void Meh()
    {
    }
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public class Foo : IDisposable
{
    private readonly Stream stream = File.OpenRead(string.Empty);
    private bool disposed;

    public Foo()
    {
    }

    public int Value { get; }

    public int this[int value]
    {
        get
        {
            return value;
        }
    }

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

    protected virtual void Bar()
    {
    }

    protected void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }
    }

    private void Meh()
    {
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 1, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableSealedClassUsingsInside()
        {
            var testCode = @"
namespace Tests
{
    using System.IO;

    public sealed class Foo
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
namespace Tests
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

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableSealedClassUsingsOutside()
        {
            var testCode = @"
using System.IO;
namespace Tests
{
    public sealed class Foo
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;
namespace Tests
{
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

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableSealedClassUnderscore()
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    ↓private readonly Stream _stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream _stream = File.OpenRead(string.Empty);
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
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableSealedClassUnderscoreWithConst()
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    public const int Value = 2;

    ↓private readonly Stream _stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public sealed class Foo : IDisposable
{
    public const int Value = 2;

    private readonly Stream _stream = File.OpenRead(string.Empty);
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
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableAbstractClass()
        {
            var testCode = @"
using System.IO;

public abstract class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public abstract class Foo : IDisposable
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
        }
    }

    protected void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableAbstractClassUnderscore()
        {
            var testCode = @"
using System.IO;

public abstract class Foo
{
    ↓private readonly Stream _stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public abstract class Foo : IDisposable
{
    private readonly Stream _stream = File.OpenRead(string.Empty);
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
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableWhenInterfaceIsMissing()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream = File.OpenRead(string.Empty);

    public void Dispose()
    {
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethod()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
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
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethodWithProtectedPrivateSetProperty()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
    protected int Value { get; private set; }
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
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
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethodWithPublicVirtualMethod()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
    public virtual void Bar()
    {
    }
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private bool disposed;

    public void Bar()
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
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethodWithProtectedVirtualMethod()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
    protected virtual void Bar()
    {
    }
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
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

    private void Bar()
    {
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableWithProperty()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    public Foo()
    {
    }

    ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private bool disposed;

    public Foo()
    {
    }

    public Stream Stream { get; } = File.OpenRead(string.Empty);

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
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task OverrideDispose()
        {
            var baseCode = @"
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
}";
            var testCode = @"
using System.IO;

public class Foo : BaseClass
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { baseCode, testCode }, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
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
}";
            await this.VerifyCSharpFixAsync(new[] { baseCode, testCode }, new[] { baseCode, fixedCode }, allowNewCompilerDiagnostics: true)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task OverrideDisposeUnderscore()
        {
            var baseCode = @"
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
}";
            var testCode = @"
using System.IO;

public class Foo : BaseClass
{
    ↓private readonly Stream _stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { baseCode, testCode }, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
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
}";
            await this.VerifyCSharpFixAsync(new[] { baseCode, testCode }, new[] { baseCode, fixedCode }, allowNewCompilerDiagnostics: true)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task VirtualDispose()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
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

    protected void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 1)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task FactoryMethodCallingPrivateCtorWithCreatedDisposable()
        {
            var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";
            var testCode = @"
using System;

public sealed class Foo
{
    ↓private readonly IDisposable value;

    private Foo(IDisposable value)
    {
        this.value = value;
    }

    public static Foo Create() => new Foo(new Disposable());
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Issue111PartialUserControl()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;

    public partial class CodeTabView : UserControl
    {
        ↓private readonly RoslynSandbox.Disposable disposable = new RoslynSandbox.Disposable();
    }
}";
            var expected = this.CSharpDiagnostic(IDISP006ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, testCode }, expected)
                      .ConfigureAwait(false);
        }

        internal override IEnumerable<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            return base.GetCSharpDiagnosticAnalyzers()
                       .Concat(new[] { CS0535Analyzer.Default });
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        //// ReSharper disable once InconsistentNaming
        private class CS0535Analyzer : DiagnosticAnalyzer
        {
            public static readonly CS0535Analyzer Default = new CS0535Analyzer();

            private CS0535Analyzer()
            {
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(
                    new DiagnosticDescriptor(
                        id: "CS0535",
                        title: string.Empty,
                        messageFormat: "'Foo' does not implement interface member 'IDisposable.Dispose()'",
                        category: string.Empty,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: false));

            public override void Initialize(AnalysisContext context)
            {
            }
        }
    }
}