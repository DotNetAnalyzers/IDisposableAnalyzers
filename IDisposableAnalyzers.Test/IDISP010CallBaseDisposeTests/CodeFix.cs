namespace IDisposableAnalyzers.Test.IDISP010CallBaseDisposeTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeMethodAnalyzer();
        private static readonly CodeFixProvider Fix = new AddBaseCallFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP010");

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
        public void WhenNotCallingBaseDisposeWithBaseCode()
        {
            var fooBaseCode = @"
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
    public class Foo : FooBase
    {
        ↓protected override void Dispose(bool disposing)
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo : FooBase
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, fooBaseCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, fooBaseCode, testCode }, fixedCode);
        }

        [Test]
        public void WhenNotCallingBaseDisposeWithoutBaseCode()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : StreamReader
    {
        public Foo(Stream stream)
            : base(stream)
        {
        }

        ↓protected override void Dispose(bool disposing)
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo : StreamReader
    {
        public Foo(Stream stream)
            : base(stream)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
        }
    }
}
