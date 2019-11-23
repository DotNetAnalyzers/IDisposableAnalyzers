namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class DisposableWalkerTests
    {
        public static class Disposes
        {
            [Test]
            public static void WhenNotUsed()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            var disposable = File.OpenRead(fileName);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(false, DisposableWalker.Disposes(symbol, semanticModel, CancellationToken.None));
            }

            [TestCase("disposable.Dispose()")]
            [TestCase("disposable?.Dispose()")]
            [TestCase("(disposable as IDisposable)?.Dispose()")]
            [TestCase("((IDisposable)disposable)?.Dispose()")]
            public static void WhenDisposed(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            var disposable = File.OpenRead(fileName);
            disposable.Dispose();
        }
    }
}".AssertReplace("disposable.Dispose()", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(true, DisposableWalker.Disposes(symbol, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void WhenUsing()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            using (var disposable = File.OpenRead(fileName))
            {
            }
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("disposable");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(true, DisposableWalker.Disposes(symbol, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void WhenUsingAfterDeclaration()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            var disposable = File.OpenRead(fileName);
            using (disposable)
            {
            }
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(true, DisposableWalker.Disposes(symbol, semanticModel, CancellationToken.None));
            }
        }
    }
}
