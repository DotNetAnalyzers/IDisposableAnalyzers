namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class CodeFix : CodeFixVerifier<IDISP003DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>
    {
        [Test]
        public async Task NotDisposingVariable()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        var stream = File.OpenRead(string.Empty);
        ↓stream = File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            // keeping it safe and doing ?.Dispose()
            // will require some work to figure out if it can be null
            var fixedCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        var stream = File.OpenRead(string.Empty);
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingVariableOfTypeObject()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        object stream = File.OpenRead(string.Empty);
        ↓stream = File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public class Foo
{
    public void Meh()
    {
        object stream = File.OpenRead(string.Empty);
        (stream as IDisposable)?.Dispose();
        stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AssigningParameterTwice()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    public void Bar(Stream stream)
    {
        stream = File.OpenRead(string.Empty);
        ↓stream = File.OpenRead(string.Empty);
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    public void Bar(Stream stream)
    {
        stream = File.OpenRead(string.Empty);
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AssigningInIfElse()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        Stream stream = File.OpenRead(string.Empty);
        if (true)
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
        else
        {
            ↓stream = File.OpenRead(string.Empty);
        }
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        Stream stream = File.OpenRead(string.Empty);
        if (true)
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
        else
        {
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingInitializedFieldInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    private readonly Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
        ↓this.stream = File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    private readonly Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
        this.stream?.Dispose();
        this.stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingInitializedPropertyInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    public Foo()
    {
        ↓this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream { get; } = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    public Foo()
    {
        this.Stream?.Dispose();
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream { get; } = File.OpenRead(string.Empty);
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingInitializedBackingFieldInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
        ↓this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream
    {
        get { return this.stream; }
        private set { this.stream = value; }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
        this.stream?.Dispose();
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream
    {
        get { return this.stream; }
        private set { this.stream = value; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingBackingFieldInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
        ↓this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream
    {
        get { return this.stream; }
        private set { this.stream = value; }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
        this.stream?.Dispose();
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream
    {
        get { return this.stream; }
        private set { this.stream = value; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldInMethod()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public void Meh()
    {
        ↓stream = File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public void Meh()
    {
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldInLambda()
        {
            var testCode = @"
using System;
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Bar += (o, e) => ↓this.Stream = File.OpenRead(string.Empty);
        }

        public event EventHandler Bar;

        public Stream Stream
        {
            get
            {
                return this.stream;
            }

            private set
            {
                this.stream = value;
            }
        }
    }
}
";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Bar += (o, e) => this.Stream = File.OpenRead(string.Empty);
        }

        public event EventHandler Bar;

        public Stream Stream
        {
            get
            {
                return this.stream;
            }

            private set
            {
                this.stream = value;
            }
        }
    }
}
";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldAssignedInReturnMethodStatementBody()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public IDisposable Meh()
    {
        return ↓this.stream = File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public IDisposable Meh()
    {
        this.stream?.Dispose();
        return this.stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldAssignedInReturnMethodExpressionBody()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public IDisposable Meh() => ↓this.stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

////            var fixedCode = @"
////using System;
////using System.IO;

////public class Foo
////{
////    private Stream stream;

////    public IDisposable Meh()
////    {
////        this.stream?.Dispose();
////        return this.stream = File.OpenRead(string.Empty);
////    }
////}";
            // Not implementing the fix for now, not a common case.
            await this.VerifyCSharpFixAsync(testCode, testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldAssignedInReturnStatementInPropertyStamementBody()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public IDisposable Meh
    {
        get
        {
            return ↓this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public IDisposable Meh
    {
        get
        {
            this.stream?.Dispose();
            return this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldAssignedInReturnStatementInPropertyExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public IDisposable Meh => ↓this.stream = File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose previous before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

////            var fixedCode = @"
////namespace RoslynSandbox
////{
////    using System;
////    using System.IO;

////    public class Foo
////    {
////        private Stream stream;

////        public IDisposable Meh
////        {
////            get
////            {
////                this.stream?.Dispose();
////                return this.stream = File.OpenRead(string.Empty);
////            }
////        }
////    }
////}";
            // Not implementing the fix for now, not a common case.
            await this.VerifyCSharpFixAsync(testCode, testCode).ConfigureAwait(false);
        }
    }
}