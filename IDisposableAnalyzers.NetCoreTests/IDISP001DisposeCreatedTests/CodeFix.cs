namespace IDisposableAnalyzers.NetCoreTests.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new LocalDeclarationAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);
        private static readonly CodeFixProvider Fix = new AddUsingFix();

        [TestCase("await task")]
        [TestCase("await task.ConfigureAwait(true)")]
        [TestCase("task.Result")]
        [TestCase("task.GetAwaiter().GetResult()")]
        public static void HttpClientIssue242(string expression)
        {
            var before = @"
namespace N
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public static  class C
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task M()
        {
            await Task.Delay(10);
            var task = HttpClient.GetAsync(""http://example.com"");
            ↓var response = await task;
        }
    }
}".AssertReplace("await task", expression);

            var after = @"
namespace N
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public static  class C
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task M()
        {
            await Task.Delay(10);
            var task = HttpClient.GetAsync(""http://example.com"");
            using var response = await task;
        }
    }
}".AssertReplace("await task", expression);
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
        }
    }
}
