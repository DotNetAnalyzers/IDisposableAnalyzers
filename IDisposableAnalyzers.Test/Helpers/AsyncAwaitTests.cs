namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public class AsyncAwaitTests
    {
        [TestCase("Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", false, null)]
        [TestCase("Task.FromResult(new string(' ', 1))", true, "new string(' ', 1)")]
        [TestCase("Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", true, "new string(' ', 1)")]
        public void TryAwaitTaskFromResult(string code, bool expected, string expectedCode)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Threading.Tasks;

    internal class Foo
    {
        internal async Task Bar()
        {
            var value = // Meh();
        }
    }
}";
            testCode = testCode.AssertReplace("// Meh()", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value as InvocationExpressionSyntax;
            Assert.AreEqual(expected, AsyncAwait.TryAwaitTaskFromResult(value, semanticModel, CancellationToken.None, out ExpressionSyntax result));
            Assert.AreEqual(expectedCode, result?.ToFullString());
        }

        [TestCase("Task.Run(() => 1)", true, "() => 1")]
        [TestCase("Task.Run(() => 1).ConfigureAwait(false)", true, "() => 1")]
        [TestCase("Task.Run(() => new string(' ', 1))", true, "() => new string(' ', 1)")]
        [TestCase("Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", true, "() => new string(' ', 1)")]
        [TestCase("Task.Run(() => CreateString())", true, "() => CreateString()")]
        [TestCase("Task.Run(() => CreateString()).ConfigureAwait(false)", true, "() => CreateString()")]
        [TestCase("Task.FromResult(new string(' ', 1))", false, null)]
        [TestCase("Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", false, null)]
        public void TryAwaitTaskRun(string code, bool expected, string expectedCode)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Threading.Tasks;

    internal class Foo
    {
        internal async Task Bar()
        {
            var value = // Meh();
        }

        internal static string CreateString() => new string(' ', 1);
    }
}";
            testCode = testCode.AssertReplace("// Meh()", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value as InvocationExpressionSyntax;
            Assert.AreEqual(expected, AsyncAwait.TryAwaitTaskRun(value, semanticModel, CancellationToken.None, out ExpressionSyntax result));
            Assert.AreEqual(expectedCode, result?.ToFullString());
        }
    }
}