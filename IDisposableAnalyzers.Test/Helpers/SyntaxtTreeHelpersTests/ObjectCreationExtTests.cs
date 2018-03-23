namespace IDisposableAnalyzers.Test.Helpers.SyntaxtTreeHelpersTests
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public class ObjectCreationExtTests
    {
        [Test]
        public void Creates()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            var foo = new Foo();
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var objectCreation = syntaxTree.FindObjectCreationExpression("new Foo()");
            var ctor = syntaxTree.FindConstructorDeclaration("public Foo()");
            Assert.AreEqual(true, objectCreation.Creates(ctor, Search.TopLevel, semanticModel, CancellationToken.None));
        }
    }
}
