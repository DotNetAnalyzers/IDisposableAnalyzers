namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFixCreateAndAssignField : CodeFixVerifier<IDISP001DisposeCreated, CreateAndAssignFieldCodeFixProvider>
    {
        [Test]
        public async Task LocalExplictTypeToFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal sealed class Foo
    {
        internal Foo()
        {
            ↓Stream stream = File.OpenRead(string.Empty);
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

    internal sealed class Foo
    {
        private readonly Stream stream;

        internal Foo()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task LocalVarToFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public Foo()
        {
            ↓var stream = File.OpenRead(string.Empty);
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
        private readonly FileStream stream;

        public Foo()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}