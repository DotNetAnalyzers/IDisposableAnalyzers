namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Ignore
        {
            [TestCase("disposables.First();")]
            [TestCase("disposables.First(x => x != null);")]
            [TestCase("disposables.Where(x => x != null);")]
            [TestCase("disposables.Single();")]
            [TestCase("Enumerable.Empty<IDisposable>();")]
            public void Linq(string linq)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Linq;

    public sealed class Foo
    {
        public Foo(IDisposable[] disposables)
        {
            var first = disposables.First();
        }
    }
}";
                testCode = testCode.AssertReplace("disposables.First();", linq);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void MockOf()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using Moq;
    using NUnit.Framework;

    public sealed class Foo
    {
        [Test]
        public void Test()
        {
            var mocked = Mock.Of<IDisposable>();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void Ninject()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using Ninject;

    public sealed class Foo
    {
        public Foo(IKernel kernel)
        {
            var mocked = kernel.Get<IDisposable>();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }
        }
    }
}