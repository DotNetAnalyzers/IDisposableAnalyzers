namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public class TestFixture
        {
            [Test]
            public void DisposingFieldInTearDown()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Tests
    {
        private Disposable disposable;

        [SetUp]
        public void SetUp()
        {
            this.disposable = new Disposable();
        }

        [TearDown]
        public void TearDown()
        {
            this.disposable.Dispose();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
            }

            [Test]
            public void DisposingFieldInOneTimeTearDown()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Tests
    {
        private Disposable disposable;

        [OneTimeSetUp]
        public void SetUp()
        {
            this.disposable = new Disposable();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.disposable.Dispose();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
            }
        }
    }
}
