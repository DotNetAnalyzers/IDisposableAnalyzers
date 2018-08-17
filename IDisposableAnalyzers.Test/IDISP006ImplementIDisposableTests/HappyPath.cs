#pragma warning disable SA1203 // Constants must appear before fields
namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP006");

        private const string DisposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        [Test]
        public void WhenCallingBaseDispose()
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class FooBase : IDisposable
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.disposable.Dispose();
            }
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : FooBase
    {
        private readonly IDisposable disposable = new Disposable();
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, ExpectedDiagnostic, DisposableCode, baseCode, testCode);
        }
    }
}
