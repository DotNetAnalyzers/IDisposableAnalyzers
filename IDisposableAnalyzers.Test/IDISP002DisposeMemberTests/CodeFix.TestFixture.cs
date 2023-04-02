namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class CodeFix
{
    public static class TestFixture
    {
        private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new();
        private static readonly DisposeInTearDownFix Fix = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP002DisposeMember);

        [Test]
        public static void AssigningFieldInSetUpWhenNoTearDown()
        {
            var before = @"
namespace N
{
    using NUnit.Framework;

    public class Tests
    {
        ↓private Disposable? disposable;

        [SetUp]
        public void SetUp()
        {
            this.disposable = new Disposable();
        }

        [Test]
        public void Test()
        {
        }
    }
}";

            var after = @"
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

        [Test]
        public void Test()
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
        }

        [Test]
        public static void AssigningFieldInOneTimeSetUp()
        {
            var before = @"
namespace N
{
    using NUnit.Framework;

    public class Tests
    {
        ↓private Disposable? disposable;

        [OneTimeSetUp]
        public void SetUp()
        {
            this.disposable = new Disposable();
        }

        [Test]
        public void Test()
        {
        }
    }
}";

            var after = @"
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
        public void OneTimeTearDown()
        {
            this.disposable?.Dispose();
        }

        [Test]
        public void Test()
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
        }

        [Test]
        public static void AssigningFieldInOneTimeSetUpWhenOneTimeTearDownExists()
        {
            var before = @"
namespace N
{
    using NUnit.Framework;

    public class Tests
    {
        ↓private Disposable? disposable;

        [OneTimeSetUp]
        public void SetUp()
        {
            this.disposable = new Disposable();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
        }

        [Test]
        public void Test()
        {
        }
    }
}";

            var after = @"
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

        [Test]
        public void Test()
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
        }
    }
}
