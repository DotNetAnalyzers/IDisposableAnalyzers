namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class AssignmentTests
    {
        internal class Symbol
        {
            [TestCase(Search.Recursive)]
            [TestCase(Search.TopLevel)]
            public void FieldWithCtorArg(Search search)
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
                var value = syntaxTree.FindMemberAccessExpression("this.value");
                var ctor = syntaxTree.FindConstructorDeclaration("Foo(int arg)");
                var field = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                Assert.AreEqual(true, AssignmentWalker.FirstForSymbol(field, ctor, search, semanticModel, CancellationToken.None, out AssignmentExpressionSyntax result));
                Assert.AreEqual("this.value = arg", result?.ToString());
            }

            [TestCase(Search.Recursive)]
            [TestCase(Search.TopLevel)]
            public void FieldWithChainedCtorArg(Search search)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value;

        public Foo()
            : this(1)
        {
        }

        internal Foo(int arg)
        {
            this.value = arg;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindMemberAccessExpression("this.value");
                var ctor = syntaxTree.FindConstructorDeclaration("Foo()");
                AssignmentExpressionSyntax result;
                var field = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                if (search == Search.Recursive)
                {
                    Assert.AreEqual(true, AssignmentWalker.FirstForSymbol(field, ctor, Search.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.value = arg", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, AssignmentWalker.FirstForSymbol(field, ctor, Search.TopLevel, semanticModel, CancellationToken.None, out result));
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
                var value = syntaxTree.FindMemberAccessExpression("this.number");
                var ctor = syntaxTree.FindConstructorDeclaration("Foo(int arg)");
                AssignmentExpressionSyntax result;
                var field = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                if (search == Search.Recursive)
                {
                    Assert.AreEqual(true, AssignmentWalker.FirstForSymbol(field, ctor, Search.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.number = value", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, AssignmentWalker.FirstForSymbol(field, ctor, Search.TopLevel, semanticModel, CancellationToken.None, out result));
                }
            }

            [TestCase(Search.Recursive)]
            [TestCase(Search.TopLevel)]
            public void FieldInPropertyExpressionBody(Search search)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private int number;

        internal Foo()
        {
            var i = this.Number;
        }

        public int Number => this.number = 3;
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindMemberAccessExpression("this.number");
                var ctor = syntaxTree.FindConstructorDeclaration("Foo()");
                AssignmentExpressionSyntax result;
                var field = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                if (search == Search.Recursive)
                {
                    Assert.AreEqual(true, AssignmentWalker.FirstForSymbol(field, ctor, Search.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.number = 3", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, AssignmentWalker.FirstForSymbol(field, ctor, Search.TopLevel, semanticModel, CancellationToken.None, out result));
                }
            }
        }
    }
}
