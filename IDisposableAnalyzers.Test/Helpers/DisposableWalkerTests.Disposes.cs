namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DisposableWalkerTests
    {
        public class Disposes
        {
            [Test]
            public void WhenNotUsed()
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(false, DisposableWalker.Disposes(symbol, semanticModel, CancellationToken.None, null));
            }

            [TestCase("disposable.Dispose()")]
            [TestCase("disposable?.Dispose()")]
            [TestCase("(disposable as IDisposable)?.Dispose()")]
            [TestCase("((IDisposable)disposable)?.Dispose()")]
            public void WhenDisposed(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(true, DisposableWalker.Disposes(symbol, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void WhenUsing()
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(true, DisposableWalker.Disposes(symbol, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void WhenUsingAfterDeclaration()
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(true, DisposableWalker.Disposes(symbol, semanticModel, CancellationToken.None, null));
            }
        }
    }
}
