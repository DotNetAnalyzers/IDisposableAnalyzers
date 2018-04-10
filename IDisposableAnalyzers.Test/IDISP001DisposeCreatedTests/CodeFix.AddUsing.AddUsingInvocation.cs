namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class AddUsingInvocation
        {
            private static readonly DiagnosticAnalyzer Analyzer = new IDISP001DisposeCreated();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP001");
            private static readonly CodeFixProvider Fix = new AddUsingCodeFixProvider();

            [Test]
            public void AddUsingForLocal()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            ↓var stream = File.OpenRead(string.Empty);
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AddUsingForLocalOneStatementAfter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            ↓var stream = File.OpenRead(string.Empty);
            var i = 1;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var i = 1;
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AddUsingForLocalManyStatements()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            ↓var stream = File.OpenRead(string.Empty);
            var a = 1;
            var b = 1;
            if (a == b)
            {
                var c = 2;
            }
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        public void Meh()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var a = 1;
                var b = 1;
                if (a == b)
                {
                    var c = 2;
                }
            }
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
