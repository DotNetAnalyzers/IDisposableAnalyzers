namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class AddUsingForObjectCreation
        {
            private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP004");
            private static readonly CodeFixProvider Fix = new AddUsingFix();

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
            public void AddUsingForIgnoredFileOpenRead()
            {
                var testCode = @"
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

                var fixedCode = @"
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AddUsingForIgnoredReturnEmpty()
            {
                var testCode = @"
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

                var fixedCode = @"
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void AddUsingForIgnoredReturnManyStatements()
            {
                var testCode = @"
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

                var fixedCode = @"
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode }, fixedCode);
            }

            [Test]
            public void NoFixForArgument()
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

                AnalyzerAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, new[] { DisposableCode, testCode });
            }
        }
    }
}
