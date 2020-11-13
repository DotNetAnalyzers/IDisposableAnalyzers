namespace IDisposableAnalyzers.NetCoreTests.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class DisposableTests
    {
        public static class IsCreation
        {
            [TestCase("Microsoft.Extensions.Logging.ApplicationInsightsLoggerFactoryExtensions.AddApplicationInsights(((Microsoft.Extensions.Logging.ILoggerFactory)o), null)")]
            public static void WhiteList(string code)
            {
                var testCode = @"
namespace N
{
    internal class Foo
    {
        internal Foo(object o)
        {
            var value = PLACEHOLDER;
        }
    }
}".AssertReplace("PLACEHOLDER", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(code);
                Assert.AreEqual(false, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            // [TestCase("HttpClient.GetAsync(\"http://example.com\")",                             false)]
            [TestCase("await HttpClient.GetAsync(\"http://example.com\")",                       true)]
            [TestCase("await HttpClient.GetAsync(\"http://example.com\").ConfigureAwait(false)", true)]
            public static void AwaitExpression(string expression, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class C
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        internal async Task M()
        {
            var value = await HttpClient.GetAsync(""http://example.com"");
        }
    }
}".AssertReplace("await HttpClient.GetAsync(\"http://example.com\")", expression));
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(expression);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }

            [TestCase("await task",                      true)]
            [TestCase("await task.ConfigureAwait(true)", true)]
            //[TestCase("task.Result",                     true)]
            //[TestCase("task.GetAwaiter().GetResult()",   true)]
            public static void AwaitTask(string expression, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    class C
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        internal async Task M()
        {
            var task = HttpClient.GetAsync(""http://example.com"");
            var value = await task;
        }
    }
}".AssertReplace("await task", expression));
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression(expression);
                Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
