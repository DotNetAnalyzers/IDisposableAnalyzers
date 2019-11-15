namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public static class AsyncAwaitTests
    {
        [TestCase("Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", false, null)]
        [TestCase("System.Threading.Tasks.Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", false, null)]
        [TestCase("Task.FromResult(new string(' ', 1))", true, "new string(' ', 1)")]
        [TestCase("Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", true, "new string(' ', 1)")]
        [TestCase("System.Threading.Tasks.Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", true, "new string(' ', 1)")]
        public static void TryAwaitTaskFromResult(string expression, bool expected, string expectedCode)
        {
            var code = @"
namespace N
{
    using System.Threading.Tasks;

    internal class C
    {
        internal async Task M()
        {
            var value = // Meh();
        }
    }
}".AssertReplace("// Meh()", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value as InvocationExpressionSyntax;
            Assert.AreEqual(expected, AsyncAwait.TryAwaitTaskFromResult(value, semanticModel, CancellationToken.None, out var result));
            Assert.AreEqual(expectedCode, result?.ToFullString());
        }

        [TestCase("Task.Run(() => 1)", true, "() => 1")]
        [TestCase("System.Threading.Tasks.Task.Run(() => 1)", true, "() => 1")]
        [TestCase("Task.Run(() => 1).ConfigureAwait(false)", true, "() => 1")]
        [TestCase("Task.Run(() => new string(' ', 1))", true, "() => new string(' ', 1)")]
        [TestCase("Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", true, "() => new string(' ', 1)")]
        [TestCase("Task.Run(() => CreateString())", true, "() => CreateString()")]
        [TestCase("Task.Run(() => CreateString()).ConfigureAwait(false)", true, "() => CreateString()")]
        [TestCase("System.Threading.Tasks.Task.Run(() => CreateString()).ConfigureAwait(false)", true, "() => CreateString()")]
        [TestCase("Task.FromResult(new string(' ', 1))", false, null)]
        [TestCase("Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", false, null)]
        public static void TryAwaitTaskRun(string expression, bool expected, string expectedCode)
        {
            var code = @"
namespace N
{
    using System.Threading.Tasks;

    internal class C
    {
        internal async Task M()
        {
            var value = // Meh();
        }

        internal static string CreateString() => new string(' ', 1);
    }
}".AssertReplace("// Meh()", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value as InvocationExpressionSyntax;
            Assert.AreEqual(expected, AsyncAwait.TryAwaitTaskRun(value, semanticModel, CancellationToken.None, out var result));
            Assert.AreEqual(expectedCode, result?.ToFullString());
        }
    }
}
