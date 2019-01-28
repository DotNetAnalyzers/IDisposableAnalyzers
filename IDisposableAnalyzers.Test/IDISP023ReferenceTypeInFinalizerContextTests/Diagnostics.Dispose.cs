namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using System.Text;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class Dispose
        {
            private static readonly DiagnosticAnalyzer Analyzer = new DisposeMethodAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP023ReferenceTypeInFinalizerContext.Descriptor);

            private static readonly StringBuilder Builder = new StringBuilder();

            [TestCase("Builder.Append(1)")]
            [TestCase("Builder.Length")]
            public void Static(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class C : IDisposable
    {
        private static readonly StringBuilder Builder = new StringBuilder();

        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                }

                ↓Builder.Append(1);
            }
        }
    }
}".AssertReplace("Builder.Append(1)", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("this.↓builder.Append(1)")]
            [TestCase("↓builder.Append(1)")]
            [TestCase("↓builder.Length")]
            [TestCase("↓disposable.Dispose()")]
            [TestCase("↓disposable?.Dispose()")]
            [TestCase("this.↓disposable.Dispose()")]
            [TestCase("this.↓disposable?.Dispose()")]
            [TestCase("↓Disposable.Dispose()")]
            [TestCase("↓Disposable?.Dispose()")]
            [TestCase("this.↓Disposable.Dispose()")]
            [TestCase("this.↓Disposable?.Dispose()")]
            public void InstanceOutsideIfDispose(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Text;

    public class C : IDisposable
    {
        private readonly StringBuilder builder = new StringBuilder();
        private readonly IDisposable disposable = File.OpenRead(string.Empty);

        private bool isDisposed = false;

        private readonly IDisposable Disposable { get; } = OpenRead(string.Empty);

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                }

                this.↓builder.Append(1);
            }
        }
    }
}".AssertReplace("this.↓builder.Append(1)", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [TestCase("this.↓builder.Append(1)")]
            [TestCase("↓builder.Append(1)")]
            [TestCase("↓builder.Length")]
            [TestCase("↓disposable.Dispose()")]
            [TestCase("↓disposable?.Dispose()")]
            [TestCase("this.↓disposable.Dispose()")]
            [TestCase("this.↓disposable?.Dispose()")]
            [TestCase("↓Disposable.Dispose()")]
            [TestCase("↓Disposable?.Dispose()")]
            [TestCase("this.↓Disposable.Dispose()")]
            [TestCase("this.↓Disposable?.Dispose()")]
            public void InstanceNoIfDispose(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Text;

    public class C : IDisposable
    {
        private readonly StringBuilder builder = new StringBuilder();
        private readonly IDisposable disposable = File.OpenRead(string.Empty);

        private readonly IDisposable Disposable { get; } = OpenRead(string.Empty);

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            this.↓builder.Append(1);
        }
    }
}".AssertReplace("this.↓builder.Append(1)", expression);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void CallingStatic()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class C : IDisposable
    {
        private static readonly StringBuilder Builder = new StringBuilder();

        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                }

                ↓M();
            }
        }

        private static void M() => Builder.Append(1);
    }
}";

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
