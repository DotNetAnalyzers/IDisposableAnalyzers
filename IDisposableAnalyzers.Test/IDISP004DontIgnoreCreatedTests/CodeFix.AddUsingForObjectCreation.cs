namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class AddUsingForObjectCreation
        {
            private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP004");
            private static readonly CodeFixProvider Fix = new AddUsingFix();

            private const string Disposable = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

            [Test]
            public static void AddUsingForIgnoredFileOpenRead()
            {
                var before = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        public void Meh()
        {
            ↓new Disposable();
            var i = 1;
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        public void Meh()
        {
            using (new Disposable())
            {
                var i = 1;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void AddUsingForIgnoredReturnEmpty()
            {
                var before = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        public void Meh()
        {
            ↓new Disposable();
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        public void Meh()
        {
            using (new Disposable())
            {
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void AddUsingForIgnoredReturnManyStatements()
            {
                var before = @"
namespace RoslynSandbox
{
    using System;

    public sealed class C
    {
        public void Meh()
        {
            ↓new Disposable();
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

    public sealed class C
    {
        public void Meh()
        {
            using (new Disposable())
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void NoFixForArgument()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        internal static string M()
        {
            return Meh(↓new Disposable());
        }

        private static string Meh(IDisposable stream) => stream.ToString();
    }
}";

                RoslynAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, testCode });
            }
        }
    }
}
