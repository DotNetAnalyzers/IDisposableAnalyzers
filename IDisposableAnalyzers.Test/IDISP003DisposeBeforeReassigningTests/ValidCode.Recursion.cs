namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public partial class ValidCode<T>
    {
        [Test]
        public void IgnoresWhenDisposingRecursiveProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.RecursiveProperty.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresWhenNotDisposingRecursiveProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresWhenDisposingFieldAssignedWithRecursiveProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private IDisposable disposable;

        public Foo()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresWhenNotDisposingFieldAssignedWithRecursiveProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo : IDisposable
    {
        private IDisposable disposable;

        public Foo()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresWhenDisposingRecursiveMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public IDisposable RecursiveMethod() => RecursiveMethod();

        public void Dispose()
        {
            this.RecursiveMethod().Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void RecursiveOut()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class Foo
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
