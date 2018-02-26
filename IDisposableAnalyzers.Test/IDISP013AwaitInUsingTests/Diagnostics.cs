namespace IDisposableAnalyzers.Test.IDISP013AwaitInUsingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly ReturnValueAnalyzer Analyzer = new ReturnValueAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP013");

        [Test]
        public void NotAwaitingReturnInvocationInUsing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class Foo
    {
        public Task<string> Bar()
        {
            using (var client = new WebClient())
            {
                return ↓client.DownloadStringTaskAsync(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Explicit("Fix #48")]
        [Test]
        public void NotAwaitingInUsing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class Foo
    {
        public Task<string> Bar()
        {
            using (var client = new WebClient())
            {
                var task = client.DownloadStringTaskAsync(string.Empty);
                return ↓task;
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
