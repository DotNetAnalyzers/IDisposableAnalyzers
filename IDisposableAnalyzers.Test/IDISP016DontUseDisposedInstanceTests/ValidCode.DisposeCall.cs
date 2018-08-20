namespace IDisposableAnalyzers.Test.IDISP016DontUseDisposedInstanceTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class ValidCode
    {
        internal class DisposeCall
        {
            private static readonly DiagnosticAnalyzer Analyzer = new DisposeCallAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP016");

            [Test]
            public void CreateTouchDispose()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public void Bar()
        {
            var stream = File.OpenRead(string.Empty);
            var b = stream.ReadByte();
            stream.Dispose();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void UsingFileOpenRead()
            {
                var testCode = @"
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
                AnalyzerAssert.Valid(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void DisposeInUsing()
            {
                // this is weird but should not warn I think
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                stream.Dispose();
            }
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
