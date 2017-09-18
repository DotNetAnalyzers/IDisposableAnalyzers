namespace IDisposableAnalyzers.Test.Helpers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal partial class DisposableTests
    {
        internal class IsPotentiallyAssignableTo
        {
            [TestCase("1", false)]
            [TestCase("null", false)]
            [TestCase("System.StringComparison.CurrentCulture", false)]
            [TestCase("\"abc\"", false)]
            public void ShortCircuit(string code, bool expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
            var value = PLACEHOLDER;
        }
    }
}";
                testCode = testCode.AssertReplace("PLACEHOLDER", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
                Assert.AreEqual(expected, Disposable.IsPotentiallyAssignableTo(value, null, CancellationToken.None));
            }

            [TestCase("new string(' ', 1)", false)]
            public void ObjectCreation(string code, bool expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private const int ConstInt = 1;

        internal Foo()
        {
            var value = PLACEHOLDER;
        }

        public int Value { get; } = 2;
    }
}";
                testCode = testCode.AssertReplace("PLACEHOLDER", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
                Assert.AreEqual(expected, Disposable.IsPotentiallyAssignableTo(value, semanticModel, CancellationToken.None));
            }
        }
    }
}