namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public class Finalizer
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FinalizerAnalyzer();

            [Test]
            public void SealedWithFinalizerStatementBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
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
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void SealedWithFinalizerExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDisposed = false;

        ~C() =>this.Dispose(false);

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
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [TestCase("isDisposed.Equals(false)")]
            [TestCase("isDisposed.Equals(this)")]
            public void TouchingStruct(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
            _ = isDisposed.Equals(false);
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
            }
        }
    }
}".AssertReplace("isDisposed.Equals(false)", expression);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void SettingStaticToNull()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class C
    {
        private static StringBuilder Builder = new StringBuilder();

        ~C()
        {
             Builder = null;
        }
    }
}";

                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void SettingInstanceToNull()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class C
    {
        private StringBuilder Builder = new StringBuilder();

        ~C()
        {
             this.Builder = null;
        }
    }
}";

                AnalyzerAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
