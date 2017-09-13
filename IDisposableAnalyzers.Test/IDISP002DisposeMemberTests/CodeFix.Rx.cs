namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class CodeFix : CodeFixVerifier<IDISP002DisposeMember, DisposeMemberCodeFixProvider>
    {
        internal class Rx : NestedCodeFixVerifier<CodeFix>
        {
            [Test]
            public async Task SerialDisposable()
            {
                var testCode = @"
using System;
using System.IO;
using System.Reactive.Disposables;

public sealed class Foo : IDisposable
{
    ↓private readonly SerialDisposable disposable = new SerialDisposable();

    public void Update()
    {
        this.disposable.Disposable = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
    }
}";
                var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Dispose member.");
                await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

                var fixedCode = @"
using System;
using System.IO;
using System.Reactive.Disposables;

public sealed class Foo : IDisposable
{
    private readonly SerialDisposable disposable = new SerialDisposable();

    public void Update()
    {
        this.disposable.Disposable = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
                await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
            }

            [Test]
            public async Task ObservableSubscribe()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
        }
    }
}";
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Dispose member.");
                await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IObservable<object> observable)
        {
            this.disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
            }
        }
    }
}