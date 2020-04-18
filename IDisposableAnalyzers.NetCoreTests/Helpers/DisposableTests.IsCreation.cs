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
                Assert.AreEqual(Result.No, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
