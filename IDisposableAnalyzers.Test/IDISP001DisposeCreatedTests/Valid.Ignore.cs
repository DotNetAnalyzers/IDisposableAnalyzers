namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public static partial class Valid<T>
    {
        [TestCase("disposables.First();")]
        [TestCase("disposables.First(x => x != null);")]
        [TestCase("disposables.Where(x => x != null);")]
        [TestCase("disposables.Single();")]
        [TestCase("Enumerable.Empty<IDisposable>();")]
        public static void Linq(string linq)
        {
            var code = @"
namespace N
{
    using System;
    using System.Linq;

    public sealed class C
    {
        public C(IDisposable[] disposables)
        {
            var first = disposables.First();
        }
    }
}".AssertReplace("disposables.First();", linq);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MockOf()
        {
            var code = @"
namespace N
{
    using System;
    using Moq;
    using NUnit.Framework;

    public sealed class C
    {
        [Test]
        public void Test()
        {
            var mocked = Mock.Of<IDisposable>();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Ninject()
        {
            var code = @"
namespace N
{
    using System;
    using Ninject;

    public sealed class C
    {
        public C(IKernel kernel)
        {
            var mocked = kernel.Get<IDisposable>();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
