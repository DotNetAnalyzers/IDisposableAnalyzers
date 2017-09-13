namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class Diagnostics : DiagnosticVerifier<IDISP001DisposeCreated>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

        [TestCase("new Disposable()")]
        [TestCase("new Disposable() as object")]
        [TestCase("(object) new Disposable()")]
        [TestCase("File.OpenRead(string.Empty) ?? null")]
        [TestCase("null ?? File.OpenRead(string.Empty)")]
        [TestCase("true ? null : File.OpenRead(string.Empty)")]
        [TestCase("true ? File.OpenRead(string.Empty) : null")]
        public async Task LanguageConstructs(string code)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class Foo
    {
        internal Foo()
        {
            ↓var value = new Disposable();
        }
    }
}";
            testCode = testCode.AssertReplace("new Disposable()", code);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyInitializedPasswordBoxSecurePassword()
        {
            var testCode = @"
    using System.Windows.Controls;

    public class Foo
    {
        public PasswordBox PasswordBox { get; } = new PasswordBox();

        public long Bar()
        {
            ↓var pwd = PasswordBox.SecurePassword;
            return pwd.Length;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task StaticPropertyInitializedPasswordBoxSecurePassword()
        {
            var testCode = @"
    using System.Windows.Controls;

    public class Foo
    {
        public static PasswordBox PasswordBox { get; } = new PasswordBox();

        public long Bar()
        {
            ↓var pwd = PasswordBox.SecurePassword;
            return pwd.Length;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task FileOpenRead()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public long Bar()
        {
            ↓var stream = File.OpenRead(string.Empty);
            return stream.Length;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task NewDisposable()
        {
            var testCode = @"
    public static class Foo
    {
        public static long Bar()
        {
            ↓var meh = new Disposable();
            return 1;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode, DisposableCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task MethodCreatingDisposable1()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream()
        {
            return File.OpenRead(string.Empty);
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task MethodCreatingDisposable2()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task MethodCreatingDisposableExpressionBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream() => File.OpenRead(string.Empty);
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyCreatingDisposableSimple()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Stream 
        {
           get { return File.OpenRead(string.Empty); }
        }

        public static long Bar()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyCreatingDisposableGetBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Stream 
        {
           get
           {
               var stream = File.OpenRead(string.Empty);
               return stream;
           }
        }

        public static long Bar()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyCreatingDisposableExpressionBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Stream => File.OpenRead(string.Empty);

        public static long Bar()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }
    }
}