namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class Finalizer
        {
            private static readonly FinalizerAnalyzer Analyzer = new();

            [Test]
            public static void SealedWithFinalizerStatementBody()
            {
                var code = @"
namespace N
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
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void SealedWithFinalizerExpressionBody()
            {
                var code = @"
namespace N
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
                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("isDisposed.Equals(false)")]
            [TestCase("isDisposed.Equals(this)")]
            public static void TouchingStruct(string expression)
            {
                var code = @"
namespace N
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
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void SettingStaticToNull()
            {
                var code = @"
namespace N
{
    using System.Text;

    public class C
    {
        private static StringBuilder? Builder = new StringBuilder();

        ~C()
        {
             Builder = null;
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void SettingInstanceToNull()
            {
                var code = @"
namespace N
{
    using System.Text;

    public class C
    {
        private StringBuilder? Builder = new StringBuilder();

        ~C()
        {
             this.Builder = null;
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void AttributedFinalizer()
            {
                var code = @"
namespace N
{
    using System;

    [AttributeUsage(AttributeTargets.All)]
    public class FooAttribute : Attribute { }

    public sealed class C : IDisposable
    {
        [Foo]
        ~C()
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }
        }
    }
}
