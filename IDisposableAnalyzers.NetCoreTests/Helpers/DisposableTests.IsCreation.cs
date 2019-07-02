namespace IDisposableAnalyzers.NetCoreTests.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal static partial class DisposableTests
    {
        internal static class IsCreation
        {
            [TestCase("Microsoft.Extensions.Logging.ApplicationInsightsLoggerFactoryExtensions.AddApplicationInsights(((Microsoft.Extensions.Logging.ILoggerFactory)o), null)")]
            public static void WhiteList(string code)
            {
                var testCode = @"
namespace RoslynSandbox
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
