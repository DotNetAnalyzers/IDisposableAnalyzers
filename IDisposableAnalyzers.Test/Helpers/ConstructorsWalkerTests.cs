namespace IDisposableAnalyzers.Test.Helpers
{
    using System;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    internal class ConstructorsWalkerTests
    {
        [Test]
        public void TwoInternalChained()
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
            var type = syntaxTree.FindBestMatch<TypeDeclarationSyntax>("Foo");
            using (var walker = ConstructorsWalker.Borrow(type, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", walker.NonPrivateCtors.Select(c => c.ToString().Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0]));
                Assert.AreEqual("internal Foo(), internal Foo(string text)", actual);
                Assert.AreEqual(0, walker.ObjectCreations.Count);
            }
        }

        [Test]
        public void InternalPrivateChained()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        private Foo()
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
            var type = syntaxTree.FindBestMatch<TypeDeclarationSyntax>("Foo");
            using (var pooled = ConstructorsWalker.Borrow(type, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.NonPrivateCtors.Select(c => c.ToString().Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0]));
                Assert.AreEqual("internal Foo(string text)", actual);
                Assert.AreEqual(0, pooled.ObjectCreations.Count);
            }
        }

        [Test]
        public void PrivatePrivateFactory()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        private Foo()
        {
        }

        internal Foo Create()
        {
            return new Foo();
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var type = syntaxTree.FindBestMatch<TypeDeclarationSyntax>("Foo");
            using (var walker = ConstructorsWalker.Borrow(type, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", walker.NonPrivateCtors.Select(c => c.ToString().Split('\r')[0]));
                Assert.AreEqual(string.Empty, actual);
                Assert.AreEqual("new Foo()", string.Join(", ", walker.ObjectCreations));
            }
        }
    }
}