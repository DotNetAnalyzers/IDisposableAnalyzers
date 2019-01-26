namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public class Dispose
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FinalizerAnalyzer();

            [Test]
            public void TouchingReferenceTypeInIfBlock()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Text;

    public sealed class C : IDisposable
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
                    Builder.Append(1);
                }
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void TouchingReferenceTypeInIfExpression()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Text;

    public sealed class C : IDisposable
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
                    Builder.Append(1);
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

                _ = isDisposed.Equals(false);
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
    using System;
    using System.Text;

    public class C
    {
        private static StringBuilder Builder = new StringBuilder();
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

                Builder = null;
            }
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
    using System;
    using System.Text;

    public class C
    {
        private StringBuilder Builder = new StringBuilder();
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

                this.Builder = null;
            }
        }
    }
}";

                AnalyzerAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
