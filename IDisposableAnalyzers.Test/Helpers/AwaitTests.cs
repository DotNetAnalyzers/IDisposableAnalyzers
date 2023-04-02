namespace IDisposableAnalyzers.Test.Helpers;

using System.Threading;

using Gu.Roslyn.Asserts;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

public static class AwaitTests
{
    [TestCase("Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", null)]
    [TestCase("System.Threading.Tasks.Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", null)]
    [TestCase("Task.FromResult(new string(' ', 1))", "new string(' ', 1)")]
    [TestCase("Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", "new string(' ', 1)")]
    [TestCase("System.Threading.Tasks.Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", "new string(' ', 1)")]
    public static void TryAwaitTaskFromResult(string expression, string expectedCode)
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
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindEqualsValueClause(expression).Value as InvocationExpressionSyntax;
        Assert.AreEqual(expectedCode, Await.TaskFromResult(value, semanticModel, CancellationToken.None)?.ToFullString());
    }

    [TestCase("Task.Run(() => 1)", "() => 1")]
    [TestCase("System.Threading.Tasks.Task.Run(() => 1)", "() => 1")]
    [TestCase("Task.Run(() => 1).ConfigureAwait(false)", "() => 1")]
    [TestCase("Task.Run(() => new string(' ', 1))", "() => new string(' ', 1)")]
    [TestCase("Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", "() => new string(' ', 1)")]
    [TestCase("Task.Run(() => CreateString())", "() => CreateString()")]
    [TestCase("Task.Run(() => CreateString()).ConfigureAwait(false)", "() => CreateString()")]
    [TestCase("System.Threading.Tasks.Task.Run(() => CreateString()).ConfigureAwait(false)", "() => CreateString()")]
    [TestCase("Task.FromResult(new string(' ', 1))", null)]
    [TestCase("Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", null)]
    public static void TryAwaitTaskRun(string expression, string expectedCode)
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
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindEqualsValueClause(expression).Value as InvocationExpressionSyntax;
        Assert.AreEqual(expectedCode, Await.TaskRun(value, semanticModel, CancellationToken.None)?.ToFullString());
    }
}
