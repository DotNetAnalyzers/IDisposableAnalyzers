// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP014UseSingleInstanceOfHttpClientTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();

        [Test]
        public void StaticFieldAssigtnedInInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;

    public class C
    {
        public static readonly HttpClient Client = new HttpClient();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticFieldAssignedInStaticCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Net.Http;

    public class C
    {
        private static readonly HttpClient _client;

        static C()
        {
            _client = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = false }) { BaseAddress = new Uri(""http://server/"") };
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticPropertyAssignedInInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Net.Http;

    public class C
    {
        static C()
        {
            Client = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = false }) { BaseAddress = new Uri(""http://server/"") };
        }

        public static HttpClient Client { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticPropertyAssignedInStaticCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Net.Http;

    public class C
    {
        public static HttpClient Client { get; } = new HttpClient();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CustomHttpClient()
        {
            var httpClientCode = @"
namespace RoslynSandbox
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
    public class C
    {
        public C()
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
