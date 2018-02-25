// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP013AwaitInUsingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly ReturnValueAnalyzer Analyzer = new ReturnValueAnalyzer();

        [Test]
        public void AwaitingInUsing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net;
    using System.Threading.Tasks;

    public class Foo
    {
        public async Task<string> Bar()
        {
            using (var client = new WebClient())
            {
                return await client.DownloadStringTaskAsync(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
