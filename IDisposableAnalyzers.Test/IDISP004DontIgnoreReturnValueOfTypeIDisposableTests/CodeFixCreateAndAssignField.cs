namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFixCreateAndAssignField : CodeFixVerifier<IDISP004DontIgnoreReturnValueOfTypeIDisposable, CreateAndAssignFieldCodeFixProvider>
    {
        [Test]
        public async Task AssignIgnoredReturnValueToFieldInCtor()
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
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore return value of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal sealed class Foo
    {
        private readonly IDisposable disposable;

        internal Foo()
        {
            this.disposable = File.OpenRead(string.Empty);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}