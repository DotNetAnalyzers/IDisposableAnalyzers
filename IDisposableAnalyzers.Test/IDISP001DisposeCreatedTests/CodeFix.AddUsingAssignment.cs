namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class AddUsingAssignment
        {
            private static readonly AssignmentAnalyzer Analyzer = new();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);
            private static readonly AddUsingFix Fix = new();

            private const string Disposable = @"
namespace N
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
            public static void NewDisposableSplitDeclarationAndAssignment()
            {
                var before = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            IDisposable disposable;
            ↓disposable = new Disposable();
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void WhenAssigningParameter()
            {
                var before = @"
namespace N
{
    using System;

    public class C
    {
        public C(IDisposable disposable)
        {
            ↓disposable = new Disposable();
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C
    {
        public C(IDisposable disposable)
        {
            using (disposable = new Disposable())
            {
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }

            [Test]
            public static void WhenAssigningLocalInLambda()
            {
                var before = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (_, __) =>
            {
                Disposable disposable;
                ↓disposable = new Disposable();
            };
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (_, __) =>
            {
                Disposable disposable;
                using (disposable = new Disposable())
                {
                }
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Disposable, before }, after);
            }
        }
    }
}
