namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class TestFixture
        {
            // ReSharper disable once UnusedMember.Local
            // ReSharper disable once MemberHidesStaticFromOuterClass
            private static readonly DisposeInTearDownCodeFixProvider Fix = new DisposeInTearDownCodeFixProvider();

            [Test]
            public void AssigningFieldInSetUpCreatesTearDownAndDisposes()
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
            ↓this.disposable = new Disposable();
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AssigningFieldInSetUpCreatesTearDownAndDisposesExplicitDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Tests
    {
        private ExplicitDisposable disposable;

        [SetUp]
        public void SetUp()
        {
            ↓this.disposable = new ExplicitDisposable();
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
        private ExplicitDisposable disposable;

        [SetUp]
        public void SetUp()
        {
            this.disposable = new ExplicitDisposable();
        }

        [TearDown]
        public void TearDown()
        {
            (this.disposable as System.IDisposable)?.Dispose();
        }

        [Test]
        public void Test()
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { ExplicitDisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { ExplicitDisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AssigningFieldInSetUpdDisposesInTearDown()
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
            ↓this.disposable = new Disposable();
        }

        [TearDown]
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AssigningFieldInSetUpdDisposesInTearDownExplicitDisposable()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Tests
    {
        private ExplicitDisposable disposable;

        [SetUp]
        public void SetUp()
        {
            ↓this.disposable = new ExplicitDisposable();
        }

        [TearDown]
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
        private ExplicitDisposable disposable;

        [SetUp]
        public void SetUp()
        {
            this.disposable = new ExplicitDisposable();
        }

        [TearDown]
        public void TearDown()
        {
            (this.disposable as System.IDisposable)?.Dispose();
        }

        [Test]
        public void Test()
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { ExplicitDisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { ExplicitDisposableCode, testCode }, fixedCode);
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
        private Disposable disposable;

        [OneTimeSetUp]
        public void SetUp()
        {
            ↓this.disposable = new Disposable();
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
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
        private Disposable disposable;

        [OneTimeSetUp]
        public void SetUp()
        {
            ↓this.disposable = new Disposable();
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }
        }
    }
}
