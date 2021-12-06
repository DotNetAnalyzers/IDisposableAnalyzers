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
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void M()
        {
            var item = RecursiveProperty;

            using(var m = RecursiveProperty)
            {
            }

            using(RecursiveProperty)
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresRecursiveGetSetProperty()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        public C()
        {
            var temp1 = this.P;
            this.P = new Disposable();
            var temp2 = this.P;
        }

        public IDisposable P
        {
            get { return this.P; }
            set { this.P = value; }
        }

        public void M()
        {
            var temp3 = this.P;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void MethodStatementBody()
        {
            var code = @"
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MethodExpressionBody()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable Forever() => Forever();

        public void M()
        {
            var m = Forever();
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WithOptionalParameter()
        {
            var code = @"
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void GenericOut()
        {
            var code = @"
namespace N
{
    public sealed class C
    {
        public T M<T>(out T t)
            where T : struct
        {
            return M(0, out t);
        }

        public T M<T>(int _, out T t)
            where T : struct
        {
            t = default;
            return default;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
