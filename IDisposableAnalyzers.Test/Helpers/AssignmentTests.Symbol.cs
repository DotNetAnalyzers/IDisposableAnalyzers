namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class AssignmentExecutionWalkerTests
    {
        internal class Symbol
        {
            [TestCase(Scope.Recursive)]
            [TestCase(Scope.Member)]
            public void FieldWithCtorArg(Scope scope)
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
                Assert.AreEqual(true, AssignmentExecutionWalker.FirstForSymbol(field, ctor, scope, semanticModel, CancellationToken.None, out AssignmentExpressionSyntax result));
                Assert.AreEqual("this.value = arg", result?.ToString());
            }

            [TestCase(Scope.Recursive)]
            [TestCase(Scope.Member)]
            public void FieldWithChainedCtorArg(Scope scope)
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
                if (scope == Scope.Recursive)
                {
                    Assert.AreEqual(true, AssignmentExecutionWalker.FirstForSymbol(field, ctor, Scope.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.value = arg", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, AssignmentExecutionWalker.FirstForSymbol(field, ctor, Scope.Member, semanticModel, CancellationToken.None, out result));
                }
            }

            [TestCase(Scope.Recursive)]
            [TestCase(Scope.Member)]
            public void FieldWithCtorArgViaProperty(Scope search)
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
                if (search == Scope.Recursive)
                {
                    Assert.AreEqual(true, AssignmentExecutionWalker.FirstForSymbol(field, ctor, Scope.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.number = value", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, AssignmentExecutionWalker.FirstForSymbol(field, ctor, Scope.Member, semanticModel, CancellationToken.None, out result));
                }
            }

            [TestCase(Scope.Recursive)]
            [TestCase(Scope.Member)]
            public void FieldInPropertyExpressionBody(Scope search)
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
                if (search == Scope.Recursive)
                {
                    Assert.AreEqual(true, AssignmentExecutionWalker.FirstForSymbol(field, ctor, Scope.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.number = 3", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, AssignmentExecutionWalker.FirstForSymbol(field, ctor, Scope.Member, semanticModel, CancellationToken.None, out result));
                }
            }
        }
    }
}
