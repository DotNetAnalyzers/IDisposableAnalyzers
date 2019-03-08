namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class Rx
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly CodeFixProvider Fix = new DisposeMemberFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP002");

            [Test]
            public void SerialDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        ↓private readonly SerialDisposable disposable = new SerialDisposable();

        public void Update()
        {
            this.disposable.Disposable = File.OpenRead(string.Empty);
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
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
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
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void ObservableSubscribe()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        ↓private readonly IDisposable disposable;

        public C(IObservable<object> observable)
        {
            this.disposable = observable.Subscribe(_ => { });
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

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IObservable<object> observable)
        {
            this.disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
