namespace IDisposableAnalyzers.Tests.Web.IDISP017PreferUsingTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeCallAnalyzer();

        [TestCase("c.DisposeAsync()")]
        [TestCase("c.DisposeAsync().ConfigureAwait(false)")]
        public static void DisposeAsync(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    class Issue204
    {
        public async ValueTask M()
        {
            var c = new C();
            try
            {

            }
            finally
            {
                await c.DisposeAsync().ConfigureAwait(false);
            }
        }

        public class C : IAsyncDisposable
        {
            private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

            public async ValueTask DisposeAsync()
            {
                await this.disposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}".AssertReplace("c.DisposeAsync().ConfigureAwait(false)", expression);
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
