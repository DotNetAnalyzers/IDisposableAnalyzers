// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP014UseSingleInstanceOfHttpClientTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ObjectCreationAnalyzer();

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

        [Test]
        public void CustomHttpClient()
        {
            var httpClientCode = @"namespace RoslynSandbox
{
    using System;

    public sealed class HttpClient : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            using (var client = new HttpClient())
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, httpClientCode, testCode);
        }
    }
}
