// ReSharper disable InconsistentNaming
#pragma warning disable SA1203 // Constants must appear before fields
namespace IDisposableAnalyzers.NetCoreTests.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();

        [Test]
        public void ILoggerFactoryAddApplicationInsights()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Logging;

    public class Foo
    {
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory)
        {
            loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Warning);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
