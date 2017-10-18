namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal class NamesTests
    {
        [Test]
        public void DefaultsToStyleCop()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
        }

        internal Foo(string text)
            : this()
        {
        }
    }
}");

            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            Assert.AreEqual(false, syntaxTree.GetRoot().UsesUnderscore(semanticModel, CancellationToken.None));
        }

        [Test]
        public void WhenFieldIsNamedWithUnderscore()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    class Foo
    {
        int _value;
        public int Bar()  => _value = 1;
    }
}");

            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            Assert.AreEqual(true, syntaxTree.GetRoot().UsesUnderscore(semanticModel, CancellationToken.None));
        }

        [Test]
        public void WhenFieldIsNotNamedWithUnderscore()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    class Foo
    {
        int value;
        public int Bar()  => value = 1;
    }
}");

            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            Assert.AreEqual(false, syntaxTree.GetRoot().UsesUnderscore(semanticModel, CancellationToken.None));
        }

        [Test]
        public void WhenUsingThis()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    class Foo
    {
        public int Value { get; private set; }

        public int Bar()  => this.value = 1;
    }
}");

            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            Assert.AreEqual(false, syntaxTree.GetRoot().UsesUnderscore(semanticModel, CancellationToken.None));
        }

        [Test]
        public void FiguresOutFromOtherClass()
        {
            var fooCode = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    class Foo
    {
        private int _value;

        public int Bar()  => _value = 1;
    }
}");

            var barCode = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    class Bar
    {
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { fooCode, barCode }, MetadataReferences.FromAttributes());
            Assert.AreEqual(2, compilation.SyntaxTrees.Length);
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                Assert.AreEqual(true, tree.GetRoot().UsesUnderscore(semanticModel, CancellationToken.None));
            }
        }
    }
}
