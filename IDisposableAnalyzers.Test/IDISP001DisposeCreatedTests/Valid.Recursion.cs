namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public partial class Valid<T>
    {
        [Test]
        public static void IgnoresRecursiveCalculatedProperty()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Meh()
        {
            var item = RecursiveProperty;

            using(var meh = RecursiveProperty)
            {
            }

            using(RecursiveProperty)
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IgnoresRecursiveGetSetProperty()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C
    {
        public C()
        {
            var temp1 = this.M;
            this.M = new Disposable();
            var temp2 = this.M;
        }

        public IDisposable M
        {
            get { return this.M; }
            set { this.M = value; }
        }

        public void Meh()
        {
            var temp3 = this.M;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Disposable, testCode);
        }

        [Test]
        public static void MethodStatementBody()
        {
            var testCode = @"
    using System;

    public static class C
    {
        public static void M()
        {
            var disposable = Forever();
            Forever();
            using(var item = Forever())
            {
            }

            using(Forever())
            {
            }
        }

        private static IDisposable Forever()
        {
            return Forever();
        }
    }";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void MethodExpressionBody()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable Forever() => Forever();

        public void Meh()
        {
            var meh = Forever();
            Forever();

            using(var item = Forever())
            {
            }

            using(Forever())
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void WithOptionalParameter()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public abstract class C
    {
        public C(IDisposable disposable)
        {
            var value = disposable;
            value = WithOptionalParameter(value);
        }

        private static IDisposable WithOptionalParameter(IDisposable value, IEnumerable<IDisposable> values = null)
        {
            if (values == null)
            {
                return WithOptionalParameter(value, new[] { value });
            }

            return value;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
