// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP014UseSingleInstanceOfHttpClientTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly ObjectCreationAnalyzer Analyzer = new ObjectCreationAnalyzer();

        [Test]
        public void StaticField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public class Foo
    {
        public static readonly HttpClient Client = new HttpClient();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public class Foo
    {
        public static HttpClient Client { get; } = new HttpClient();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
