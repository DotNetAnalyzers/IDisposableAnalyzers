namespace IDisposableAnalyzers.Test.IDISP014UseSingleInstanceOfHttpClientTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly ObjectCreationAnalyzer Analyzer = new ObjectCreationAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP014");

        [Test]
        public void Using()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public class Foo
    {
        static async Task<HttpResponseMessage> Bar()
        {
            using(var client = ↓new HttpClient())
            {
                return await client.GetAsync(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void UsingFullyQualified()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public class Foo
    {
        static async Task<HttpResponseMessage> Bar()
        {
            using(var client = ↓new System.Net.Http.HttpClient())
            {
                return await client.GetAsync(string.Empty);
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void Field()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;

    public class Foo
    {
       private readonly HttpClient client = ↓new HttpClient();
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void Property()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;

    public class Foo
    {
       public HttpClient Client { get; } = ↓new HttpClient();
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
