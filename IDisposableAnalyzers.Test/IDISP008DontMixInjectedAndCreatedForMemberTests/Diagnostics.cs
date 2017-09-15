namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using System.Threading.Tasks;
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class Diagnostics : DiagnosticVerifier<IDISP008DontMixInjectedAndCreatedForMember>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

        [TestCase("arg ?? File.OpenRead(string.Empty)")]
        [TestCase("arg ?? File.OpenRead(string.Empty)")]
        [TestCase("File.OpenRead(string.Empty) ?? arg")]
        [TestCase("File.OpenRead(string.Empty) ?? arg")]
        [TestCase("true ? arg : File.OpenRead(string.Empty)")]
        [TestCase("true ? arg : File.OpenRead(string.Empty)")]
        [TestCase("true ? File.OpenRead(string.Empty) : arg")]
        [TestCase("true ? File.OpenRead(string.Empty) : arg")]
        public async Task InjectedAndCreatedField(string code)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

        ↓private readonly Stream stream;

        public Foo(Stream arg)
        {
            this.stream = arg ?? File.OpenRead(string.Empty);
        }
    }
}";
            testCode = testCode.AssertReplace("arg ?? File.OpenRead(string.Empty)", code);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedAndCreatedFieldCtorAndInitializer()
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);

    public Foo(Stream stream)
    {
        this.stream = stream;
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedAndCreatedFieldTwoCtors()
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    ↓private readonly Stream stream;

    public Foo()
    {
        this.stream = File.OpenRead(string.Empty);
    }

    public Foo(Stream stream)
    {
        this.stream = stream;
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        [TestCase("public Stream Stream { get; protected set; }")]
        [TestCase("public Stream Stream { get; set; }")]
        public async Task InjectedAndCreatedProperty(string property)
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    public Foo(Stream stream)
    {
        this.Stream = stream;
    }

    ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
}";
            testCode = testCode.AssertReplace("public Stream Stream { get; }", property);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedAndCreatedPropertyTwoCtors()
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    public Foo(Stream stream)
    {
        this.Stream = stream;
    }

    ↓public Stream Stream { get; }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ProtectedMutableField()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓protected Stream stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ProtectedMutableProperty()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓public Stream Stream { get; protected set; } = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task BackingFieldAssignedWithCreatedAndPropertyWithInjected()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓private Stream stream = File.OpenRead(string.Empty);

        public Foo(Stream arg)
        {
            this.Stream = arg;
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task BackingFieldAssignedWithInjectedAndPropertyWithCreated()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        ↓private Stream stream;

        public Foo(Stream arg)
        {
            this.stream = arg;
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicMethodRefParameter()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    public bool TryGetStream(ref Stream stream)
    {
        ↓stream = File.OpenRead(string.Empty);
        return true;
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }
    }
}