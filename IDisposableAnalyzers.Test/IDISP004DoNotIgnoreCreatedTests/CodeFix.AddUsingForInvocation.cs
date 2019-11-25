namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class AddUsingForInvocation
        {
            private static readonly AddUsingFix Fix = new AddUsingFix();

            [Test]
            public static void AddUsingForIgnoredFileOpenRead()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            ↓File.OpenRead(string.Empty);
            var i = 1;
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            using (File.OpenRead(string.Empty))
            {
                var i = 1;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddUsingForIgnoredReturnEmpty()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            using (File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddUsingForIgnoredReturnManyStatements()
            {
                var before = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            ↓File.OpenRead(string.Empty);
            var a = 1;
            var b = 2;
            if (a == b)
            {
                var c = 3;
            }

            var d = 4;
        }
    }
}";

                var after = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            using (File.OpenRead(string.Empty))
            {
                var a = 1;
                var b = 2;
                if (a == b)
                {
                    var c = 3;
                }

                var d = 4;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
