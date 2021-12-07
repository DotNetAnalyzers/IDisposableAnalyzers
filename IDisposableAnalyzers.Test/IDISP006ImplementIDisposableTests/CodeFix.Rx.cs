namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class Rx
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

            [Test]
            public static void ObservableElvisSubscribe()
            {
                var before = @"
namespace N
{
    using System;

    public sealed class C
    {
        ↓private readonly IDisposable? disposable;

        public C(IObservable<object> observable)
        {
            this.disposable = observable?.Subscribe(_ => { });
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable? disposable;
        private bool disposed;

        public C(IObservable<object> observable)
        {
            this.disposable = observable?.Subscribe(_ => { });
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
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
