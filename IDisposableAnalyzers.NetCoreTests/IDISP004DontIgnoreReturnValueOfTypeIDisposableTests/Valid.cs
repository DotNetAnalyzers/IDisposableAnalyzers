// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.NetCoreTests.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();

        [Test]
        public static void ILoggerFactoryAddApplicationInsights()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("var disposable = serviceProvider.GetRequiredService<Disposable>();")]
        [TestCase("_ = serviceProvider.GetRequiredService<Disposable>();")]
        [TestCase("var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();")]
        [TestCase("_ = serviceProvider.GetRequiredService<ILoggerFactory>();")]
        public static void IServiceProviderGetRequiredService(string statement)
        {
            var code = @"
namespace N
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class C
    {
        public C(IServiceProvider serviceProvider)
        {
            var disposable = serviceProvider.GetRequiredService<Disposable>();
        }

        public sealed class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}".AssertReplace("var disposable = serviceProvider.GetRequiredService<Disposable>();", statement);
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
