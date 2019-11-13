namespace IDisposableAnalyzers.Test.IDISP011DontReturnDisposedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class Recursion
        {
            [Test]
            public static void IgnoresWhenDisposingRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.RecursiveProperty.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenNotDisposingRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenDisposingFieldAssignedWithRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private IDisposable disposable;

        public C()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenNotDisposingFieldAssignedWithRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private IDisposable disposable;

        public C()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenDisposingRecursiveMethod()
            {
                var code = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable RecursiveMethod() => RecursiveMethod();

        public void Dispose()
        {
            this.RecursiveMethod().Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }
        }
    }
}
