namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class DisposableTests
    {
        public static class IsPotentiallyAssignableTo
        {
            [TestCase("1", false)]
            [TestCase("null", false)]
            [TestCase("\"abc\"", false)]
            public static void ShortCircuit(string code, bool expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class C
    {
        internal C()
        {
            var value = PLACEHOLDER;
        }
    }
}".AssertReplace("PLACEHOLDER", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                Assert.AreEqual(expected, Disposable.IsPotentiallyAssignableFrom(value, null, CancellationToken.None));
            }

            [TestCase("new string(' ', 1)", false)]
            [TestCase("new System.Text.StringBuilder()", false)]
            [TestCase("new System.IO.MemoryStream()", true)]
            public static void ObjectCreation(string code, bool expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class C
    {
        internal C()
        {
            var value = PLACEHOLDER;
        }
    }
}".AssertReplace("PLACEHOLDER", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(code).Value;
                Assert.AreEqual(expected, Disposable.IsPotentiallyAssignableFrom(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
