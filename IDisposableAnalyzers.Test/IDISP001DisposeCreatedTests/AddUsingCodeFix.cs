namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFixAddUsing : CodeFixVerifier<IDISP001DisposeCreated, AddUsingCodeFixProvider>
    {
        [Test]
        public async Task AddUsingForLocal()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            ↓var stream = File.OpenRead(string.Empty);
            var i = 1;
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var i = 1;
            }
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Explicit("Poor formatting.")]
        [Test]
        public async Task AddUsingForLocalManyStatements()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            ↓var stream = File.OpenRead(string.Empty);
            var a = 1;
            var b = 1;
            if (a == b)
            {
                var c = 2;
            }
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var a = 1;
                var b = 1;
                if (a == b)
                {
                    var c = 2;
                }
            }
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}