namespace IDisposableAnalyzers.Test.IDISP012PropertyShouldNotReturnCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP012");

        [Test]
        public static void ReturnFileOpenReadGetBody()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public Stream Meh
        {
            get
            {
                return ↓File.OpenRead(string.Empty);
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ReturnFileOpenReadExpressionBody()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public Stream Meh => ↓File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ReturnFileOpenReadGetExpressionBody()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public Stream Meh
        {
            get => ↓File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
