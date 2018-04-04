namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class AssignmentWalkerTests
    {
        internal class With
        {
            [TestCase(Search.Recursive)]
            [TestCase(Search.TopLevel)]
            public void FieldCtorArg(Search search)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value;

        internal Foo(int arg)
        {
            this.value = arg;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("this.value = arg").Right;
                var ctor = syntaxTree.FindConstructorDeclaration("Foo(int arg)");
                var arg = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                Assert.AreEqual(true, AssignmentExecutionWalker.FirstWith(arg, ctor, search, semanticModel, CancellationToken.None, out AssignmentExpressionSyntax result));
                Assert.AreEqual("this.value = arg", result?.ToString());
            }

            [TestCase(Search.Recursive)]
            [TestCase(Search.TopLevel)]
            public void FieldCtorArgInNested(Search search)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    internal class Foo
    {
        private StreamReader reader;

        internal Foo(Stream stream)
        {
            this.reader = new StreamReader(stream);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("stream");
                var ctor = syntaxTree.FindConstructorDeclaration("Foo(Stream stream)");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, AssignmentExecutionWalker.FirstWith(symbol, ctor, search, semanticModel, CancellationToken.None, out AssignmentExpressionSyntax result));
                Assert.AreEqual("this.reader = new StreamReader(stream)", result?.ToString());
            }

            [TestCase(Search.Recursive)]
            [TestCase(Search.TopLevel)]
            public void ChainedCtorArg(Search search)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value;

        public Foo(int arg)
            : this(arg, 1)
        {
        }

        internal Foo(int chainedArg, int _)
        {
            this.value = chainedArg;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var ctor = syntaxTree.FindConstructorDeclaration("Foo(int arg)");
                var symbol = semanticModel.GetDeclaredSymbolSafe(syntaxTree.FindParameter("arg"), CancellationToken.None);
                if (search == Search.Recursive)
                {
                    Assert.AreEqual(true, AssignmentExecutionWalker.FirstWith(symbol, ctor, Search.Recursive, semanticModel, CancellationToken.None, out var result));
                    Assert.AreEqual("this.value = chainedArg", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, AssignmentExecutionWalker.FirstWith(symbol, ctor, Search.TopLevel, semanticModel, CancellationToken.None, out _));
                }
            }

            [TestCase(Search.Recursive)]
            [TestCase(Search.TopLevel)]
            public void FieldWithCtorArgViaProperty(Search search)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private int number;

        internal Foo(int arg)
        {
            this.Number = arg;
        }

        public int Number
        {
            get { return this.number; }
            set { this.number = value; }
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("arg");
                var ctor = syntaxTree.FindConstructorDeclaration("Foo(int arg)");
                AssignmentExpressionSyntax result;
                var symbol = semanticModel.GetDeclaredSymbolSafe(value, CancellationToken.None);
                if (search == Search.Recursive)
                {
                    Assert.AreEqual(true, AssignmentExecutionWalker.FirstWith(symbol, ctor, Search.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.Number = arg", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, AssignmentExecutionWalker.FirstForSymbol(symbol, ctor, Search.TopLevel, semanticModel, CancellationToken.None, out result));
                }
            }
        }
    }
}
