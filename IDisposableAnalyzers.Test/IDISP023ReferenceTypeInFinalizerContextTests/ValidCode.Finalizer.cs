namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class ValidCode
    {
        public static class Finalizer
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FinalizerAnalyzer();

            [Test]
            public static void SealedWithFinalizerStatementBody()
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void SealedWithFinalizerExpressionBody()
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [TestCase("isDisposed.Equals(false)")]
            [TestCase("isDisposed.Equals(this)")]
            public static void TouchingStruct(string expression)
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
                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void SettingStaticToNull()
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

                RoslynAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public static void SettingInstanceToNull()
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

                RoslynAssert.Valid(Analyzer, testCode);
            }
        }
    }
}
