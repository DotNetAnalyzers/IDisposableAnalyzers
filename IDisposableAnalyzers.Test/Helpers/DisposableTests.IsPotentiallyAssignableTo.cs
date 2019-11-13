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
            public static void ShortCircuit(string expression, bool expected)
            {
                var code = @"
namespace N
{
    internal class C
    {
        internal C()
        {
            var value = PLACEHOLDER;
        }
    }
}".AssertReplace("PLACEHOLDER", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var value = syntaxTree.FindEqualsValueClause(expression).Value;
                Assert.AreEqual(expected, Disposable.IsPotentiallyAssignableFrom(value, null, CancellationToken.None));
            }

            [TestCase("new string(' ', 1)", false)]
            [TestCase("new System.Text.StringBuilder()", false)]
            [TestCase("new System.IO.MemoryStream()", true)]
            public static void ObjectCreation(string expression, bool expected)
            {
                var code = @"
namespace N
{
    internal class C
    {
        internal C()
        {
            var value = PLACEHOLDER;
        }
    }
}".AssertReplace("PLACEHOLDER", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindEqualsValueClause(expression).Value;
                Assert.AreEqual(expected, Disposable.IsPotentiallyAssignableFrom(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
