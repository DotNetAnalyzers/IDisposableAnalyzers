namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class TestFixture
        {
            // ReSharper disable once UnusedMember.Local
            // ReSharper disable once MemberHidesStaticFromOuterClass
            private static readonly DisposeInTearDownFix Fix = new();

            [Test]
            public static void AssigningFieldInSetUpCreatesTearDownAndDisposes()
            {
                var before = @"
namespace N
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

                var after = @"
namespace N
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void AssigningFieldInSetUpCreatesTearDownAndDisposesExplicitDisposable()
            {
                var before = @"
namespace N
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

                var after = @"
namespace N
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
            ((System.IDisposable)this.disposable)?.Dispose();
        }

        [Test]
        public void Test()
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { ExplicitDisposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { ExplicitDisposable, before }, after);
            }

            [Test]
            public static void AssigningFieldInSetUpdDisposesInTearDown()
            {
                var before = @"
namespace N
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

                var after = @"
namespace N
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void AssigningFieldInSetUpdDisposesInTearDownExplicitDisposable()
            {
                var before = @"
namespace N
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

                var after = @"
namespace N
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
            ((System.IDisposable)this.disposable)?.Dispose();
        }

        [Test]
        public void Test()
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { ExplicitDisposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { ExplicitDisposable, before }, after);
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

                var after = @"
namespace N
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void CreateStaticTeardown()
            {
                var before = @"
namespace N
{
    using NUnit.Framework;

    public static class Tests
    {
        private static Disposable disposable;

        [OneTimeSetUp]
        public static void SetUp()
        {
            ↓disposable = new Disposable();
        }

        [Test]
        public static void Test()
        {
        }
    }
}";

                var after = @"
namespace N
{
    using NUnit.Framework;

    public static class Tests
    {
        private static Disposable disposable;

        [OneTimeSetUp]
        public static void SetUp()
        {
            disposable = new Disposable();
        }

        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            disposable?.Dispose();
        }

        [Test]
        public static void Test()
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

                var after = @"
namespace N
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }
        }
    }
}
