namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class CodeFix
{
    public static class AddUsingForObjectCreation
    {
        private static readonly AddUsingFix Fix = new();

        [Test]
        public static void AddUsingForIgnoredFileOpenRead()
        {
            var before = @"
#pragma warning disable CS0219
namespace N
{
    public sealed class C
    {
        public void M()
        {
            ↓new Disposable();
            var i = 1;
        }
    }
}";

            var after = @"
#pragma warning disable CS0219
namespace N
{
    public sealed class C
    {
        public void M()
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
namespace N
{
    using System;

    public sealed class C
    {
        public void M()
        {
            ↓new Disposable();
        }
    }
}";

            var after = @"
namespace N
{
    using System;

    public sealed class C
    {
        public void M()
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
#pragma warning disable CS0219
namespace N
{
    public sealed class C
    {
        public void M()
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
#pragma warning disable CS0219
namespace N
{
    public sealed class C
    {
        public void M()
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
    }
}
