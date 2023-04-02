namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

// ReSharper disable once UnusedTypeParameter
public static partial class Valid<T>
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
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
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
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [Test]
    public static void DisposingFieldInTestCleanup()
    {
        var code = @"
namespace N
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class Tests
    {
        private Disposable? disposable;

        [TestInitialize]
        public void OnClassInitialize()
        {
            this.disposable = new Disposable();
        }

        [TestCleanup]
        public void OnClassCleanup()
        {
            this.disposable?.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [Test]
    public static void DisposingFieldInClassCleanup()
    {
        var code = @"
namespace N
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class Tests
    {
        private Disposable? disposable;

        [ClassInitialize]
        public void OnClassInitialize(TestContext testContext)
        {
            this.disposable = new Disposable();
        }

        [ClassCleanup]
        public void OnClassCleanup()
        {
            this.disposable?.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }
}
