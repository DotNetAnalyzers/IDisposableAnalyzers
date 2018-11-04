namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class AddUsingAssignment
        {
            private static readonly DiagnosticAnalyzer Analyzer = new AssignmentAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP001DisposeCreated.Descriptor);
            private static readonly CodeFixProvider Fix = new AddUsingCodeFixProvider();

            private static readonly string DisposableCode = @"
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
            public void NewDisposableSplitDeclarationAndAssignment()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            IDisposable disposable;
            ↓disposable = new Disposable();
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void WhenAssigningParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(IDisposable disposable)
        {
            ↓disposable = new Disposable();
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(IDisposable disposable)
        {
            using (disposable = new Disposable())
            {
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void WhenAssigningLocalInLambda()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            Console.CancelKeyPress += (_, __) =>
            {
                Disposable disposable;
                ↓disposable = new Disposable();
            };
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }
        }
    }
}
