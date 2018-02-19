namespace IDisposableAnalyzers.Test.IDISP011DontReturnDisposedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly ReturnValueAnalyzer Analyzer = new ReturnValueAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP011");

        [Test]
        public void ReturnFileOpenReadFromUsing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public object Meh()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return ↓stream;
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ReturnFileOpenReadDisposed()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public object Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream.Dispose();
            return ↓stream;
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ReturnLazyFromUsing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public IEnumerable<string> F()
        {
            using(var streamReader = File.OpenText(""))
                ↓return Use(streamReader);
        }

        IEnumerable<string> Use(TextReader reader)
        {
            string line;
            while((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
