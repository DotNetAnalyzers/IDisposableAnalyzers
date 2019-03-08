namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class CodeFix
    {
        public class AddUsingOutParameter
        {
            private static readonly DiagnosticAnalyzer Analyzer = new ArgumentAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP001DisposeCreated.Descriptor);
            private static readonly CodeFixProvider Fix = new AddUsingFix();

            [Test]
            public void OutParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C()
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

    public class C
    {
        public C()
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

    public class C
    {
        public C()
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

    public class C
    {
        public C()
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
