namespace IDisposableAnalyzers.Test.IDISP017PreferUsingTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeCallAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(IDISP017PreferUsing.Descriptor);
        private static readonly CodeFixProvider Fix = new AddUsingFix();

        [Test]
        public void Local()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public void Bar()
        {
            var stream = File.OpenRead(string.Empty)
            var b = stream.ReadByte();
            ↓stream.Dispose();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public void Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var b = stream.ReadByte();
            }
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void InitializedLocalDisposeInFinally()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            var stream = File.OpenRead(string.Empty);
            try
            {
                var b = stream.ReadByte();
            }
            finally
            {
                ↓stream.Dispose();
            }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var b = stream.ReadByte();
            }
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void AssignedInTryDisposeInFinally()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            FileStream stream = null;
            try
            {
                stream = File.OpenRead(string.Empty);
                var b = stream.ReadByte();
            }
            finally
            {
                ↓stream.Dispose();
            }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var b = stream.ReadByte();
            }
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }
    }
}
