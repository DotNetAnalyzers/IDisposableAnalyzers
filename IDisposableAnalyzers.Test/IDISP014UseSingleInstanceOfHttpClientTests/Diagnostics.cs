namespace IDisposableAnalyzers.Test.IDISP014UseSingleInstanceOfHttpClientTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("IDISP014");

        [Test]
        public void Using()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public class C
    {
        static async Task<HttpResponseMessage> M()
        {
            using(var client = ↓new HttpClient())
            {
                return await client.GetAsync(string.Empty);
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void UsingFullyQualified()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public class C
    {
        static async Task<HttpResponseMessage> M()
        {
            using(var client = ↓new System.Net.Http.HttpClient())
            {
                return await client.GetAsync(string.Empty);
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void Field()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;

    public class C
    {
       private readonly HttpClient client = ↓new HttpClient();
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void Property()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;

    public class C
    {
       public HttpClient Client { get; } = ↓new HttpClient();
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
