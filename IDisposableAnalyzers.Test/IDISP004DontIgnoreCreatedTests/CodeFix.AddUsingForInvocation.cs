namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class AddUsingForInvocation
        {
            private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP004DoNotIgnoreCreated);
            private static readonly AddUsingFix Fix = new AddUsingFix();

            [Test]
            public static void AddUsingForIgnoredFileOpenRead()
            {
                var before = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void Meh()
        {
            ↓File.OpenRead(string.Empty);
            var i = 1;
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void Meh()
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
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void Meh()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void Meh()
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
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void Meh()
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
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C
    {
        public void Meh()
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

            [Test]
            public static void NoFixForArgument()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        internal static string M()
        {
            return Meh(↓File.OpenRead(string.Empty));
        }

        private static string Meh(Stream stream) => stream.ToString();
    }
}";

                RoslynAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, testCode);
            }
        }
    }
}
