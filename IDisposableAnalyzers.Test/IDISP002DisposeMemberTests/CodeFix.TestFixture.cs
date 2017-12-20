namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class TestFixture
        {
            private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();
            private static readonly DisposeInTearDownCodeFixProvider CodeFix = new DisposeInTearDownCodeFixProvider();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP002");

            [Test]
            public void AssigningFieldInSetUp()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Tests
    {
        ↓private Disposable disposable;

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

                var fixedCode = @"
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
            this.disposable?.Dispose();
        }

        [Test]
        public void Test()
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AssigningFieldInOneTimeSetUp()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Tests
    {
        ↓private Disposable disposable;

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

                var fixedCode = @"
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
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AssigningFieldInOneTimeSetUpWhenOneTimeTearDownExists()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Tests
    {
        ↓private Disposable disposable;

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

                var fixedCode = @"
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
            this.disposable?.Dispose();
        }

        [Test]
        public void Test()
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, CodeFix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }
        }
    }
}
