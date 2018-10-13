namespace IDisposableAnalyzers.Test.IDISP016DontUseDisposedInstanceTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class Diagnostics
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
            stream.↓Dispose();
            var b = stream.ReadByte();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void DisposingTwice()
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
            stream.Dispose();
            stream.↓Dispose();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void DisposingTwiceInUsing()
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
                stream.Dispose();
                stream.↓Dispose();
            }
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }

            [Test]
            public void ReassignAfterDispose()
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
            stream = File.OpenRead(string.Empty);
            b = stream.ReadByte();
            stream.Dispose();
            b = ↓stream.ReadByte();
        }
    }
}";
                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
            }
        }
    }
}
