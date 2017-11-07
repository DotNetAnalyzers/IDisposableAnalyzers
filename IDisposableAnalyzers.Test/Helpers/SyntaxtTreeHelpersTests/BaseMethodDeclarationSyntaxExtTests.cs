namespace IDisposableAnalyzers.Test.Helpers.SyntaxtTreeHelpersTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    internal class BaseMethodDeclarationSyntaxExtTests
    {
        internal class TryGetMatchingParameter
        {
            [TestCase(0, "int v1")]
            [TestCase(1, "int v2")]
            [TestCase(2, "int v3")]
            public void Ordinal(int index, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    @"
namespace RoslynSandbox
{
    namespace RoslynSandbox
    {
        internal class Foo
        {
            public void Bar()
            {
                Meh(1, 2, 3);
            }

            internal void Meh(int v1, int v2, int v3)
            {
            }
        }
    }
}");
                var argument = syntaxTree.FindBestMatch<InvocationExpressionSyntax>("Meh(1, 2, 3)")
                                         .ArgumentList.Arguments[index];
                var method = syntaxTree.FindBestMatch<MethodDeclarationSyntax>("internal void Meh(int v1, int v2, int v3)");

                Assert.AreEqual(true, BaseMethodDeclarationSyntaxExt.TryGetMatchingParameter(method, argument, out var parameter));
                Assert.AreEqual(expected, parameter.ToString());
            }

            [TestCase(0, "int v1")]
            [TestCase(1, "int v2")]
            [TestCase(2, "int v3")]
            public void NamedAtOrdinalPositions(int index, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    @"
namespace RoslynSandbox
{
    namespace RoslynSandbox
    {
        internal class Foo
        {
            public void Bar()
            {
                Meh(v1: 1, v2: 2, v3: 3);
            }

            internal void Meh(int v1, int v2, int v3)
            {
            }
        }
    }
}");
                var argument = syntaxTree.FindBestMatch<InvocationExpressionSyntax>("Meh(v1: 1, v2: 2, v3: 3)")
                                         .ArgumentList.Arguments[index];
                var method = syntaxTree.FindBestMatch<MethodDeclarationSyntax>("internal void Meh(int v1, int v2, int v3)");

                Assert.AreEqual(
                    true,
                    BaseMethodDeclarationSyntaxExt.TryGetMatchingParameter(
                        method,
                        argument,
                        out var parameter));
                Assert.AreEqual(expected, parameter.ToString());
            }

            [TestCase(0, "int v2")]
            [TestCase(1, "int v1")]
            [TestCase(2, "int v3")]
            public void Named(int index, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    @"
namespace RoslynSandbox
{
    namespace RoslynSandbox
    {
        internal class Foo
        {
            public void Bar()
            {
                Meh(v2: 2, v1: 1, v3: 3);
            }

            internal void Meh(int v1, int v2, int v3)
            {
            }
        }
    }
}");
                var argument = syntaxTree.FindBestMatch<InvocationExpressionSyntax>("Meh(v2: 2, v1: 1, v3: 3)")
                                         .ArgumentList.Arguments[index];
                var method = syntaxTree.FindBestMatch<MethodDeclarationSyntax>("internal void Meh(int v1, int v2, int v3)");

                Assert.AreEqual(
                    true,
                    BaseMethodDeclarationSyntaxExt.TryGetMatchingParameter(
                        method,
                        argument,
                        out var parameter));
                Assert.AreEqual(expected, parameter.ToString());
            }

            [TestCase(0, "params int[] values")]
            [TestCase(1, "params int[] values")]
            [TestCase(2, "params int[] values")]
            public void Params(int index, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    @"
namespace RoslynSandbox
{
    namespace RoslynSandbox
    {
        internal class Foo
        {
            public void Bar()
            {
                Meh(1, 2, 3);
            }

            internal void Meh(params int[] values)
            {
            }
        }
    }
}");
                var argument = syntaxTree.FindBestMatch<InvocationExpressionSyntax>("Meh(1, 2, 3)")
                                         .ArgumentList.Arguments[index];
                var method = syntaxTree.FindBestMatch<MethodDeclarationSyntax>("internal void Meh(params int[] values)");

                Assert.AreEqual(
                    true,
                    BaseMethodDeclarationSyntaxExt.TryGetMatchingParameter(
                        method,
                        argument,
                        out var parameter));
                Assert.AreEqual(expected, parameter.ToString());
            }
        }
    }
}
