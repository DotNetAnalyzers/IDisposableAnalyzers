namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class AddUsingOutParameter
        {
            private static readonly DiagnosticAnalyzer Analyzer = new ArgumentAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP001");
            private static readonly CodeFixProvider Fix = new AddUsingCodeFixProvider();

            [Test]
            public void OutParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            Stream stream;
            if (TryGetStream(↓out stream))
            {
            }
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            Stream stream;
            if (TryGetStream(out stream))
            {
                using (stream)
                {
                }
            }
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void VarOutParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            if (TryGetStream(↓out var stream))
            {
            }
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            if (TryGetStream(out var stream))
            {
                using (stream)
                {
                }
            }
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
