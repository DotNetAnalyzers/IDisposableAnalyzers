namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public static partial class Valid<T>
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

        [Test]
        public static void RecursiveOut()
        {
            var code = @"
namespace N
{
    using System;

    public abstract class C
    {
        public static bool RecursiveOut(double foo, out IDisposable value)
        {
            value = null;
            return RecursiveOut(3.0, out value);
        }

        public void Meh()
        {
            IDisposable value;
            RecursiveOut(1.0, out value);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
