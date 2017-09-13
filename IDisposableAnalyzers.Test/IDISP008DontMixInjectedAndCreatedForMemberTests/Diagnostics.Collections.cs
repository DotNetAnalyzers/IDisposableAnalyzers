namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class Diagnostics
    {
        [Explicit("Fix later")]
        internal class Collections : NestedDiagnosticVerifier<Diagnostics>
        {
            [Test]
            public async Task ListInitializer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public sealed class Foo
    {
        private List<IDisposable> disposables;

        public void MethodName(IDisposable disposable)
        {
            ↓this.disposables = new List<IDisposable> { new Disposable(),  disposable };
        }
    }
}";
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Don't assign member with injected and created disposables.");
                await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, testCode }, expected).ConfigureAwait(false);
            }
        }
    }
}