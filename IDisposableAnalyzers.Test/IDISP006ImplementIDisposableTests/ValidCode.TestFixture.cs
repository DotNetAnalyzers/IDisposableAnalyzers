namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class TestFixture
        {
            [Test]
            public static void DisposingFieldInTearDown()
            {
                var code = @"
namespace N
{
    using NUnit.Framework;

    public class Tests
    {
        private Disposable? disposable;

        [SetUp]
        public void SetUp()
        {
            this.disposable = new Disposable();
        }

        [TearDown]
        public void TearDown()
        {
            this.disposable?.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Disposable, code);
            }

            [Test]
            public static void DisposingFieldInOneTimeTearDown()
            {
                var code = @"
namespace N
{
    using NUnit.Framework;

    public class Tests
    {
        private Disposable? disposable;

        [OneTimeSetUp]
        public void SetUp()
        {
            this.disposable = new Disposable();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.disposable?.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Disposable, code);
            }
        }
    }
}
