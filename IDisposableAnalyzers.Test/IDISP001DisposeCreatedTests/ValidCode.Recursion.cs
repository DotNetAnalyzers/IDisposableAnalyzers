namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public partial class ValidCode<T>
    {
        [Test]
        public void IgnoresRecursiveCalculatedProperty()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresRecursiveGetSetProperty()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void MethodStatementBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WithOptionalParameter()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
