namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class DisposableTests
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
                Assert.AreEqual(false, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
            }

            [TestCase("disposable.Dispose()")]
            [TestCase("disposable?.Dispose()")]
            [TestCase("(disposable as IDisposable)?.Dispose()")]
            [TestCase("((IDisposable)disposable)?.Dispose()")]
            public static void DisposeInvocation(string expression)
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
                Assert.AreEqual(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void Using()
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
                Assert.AreEqual(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void UsingDeclaration()
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
            using var disposable = File.OpenRead(fileName);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("disposable");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void UsingAfterDeclaration()
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
                Assert.AreEqual(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
            }

            [Test]
            public static void WhenAddedToFormComponents()
            {
                var code = @"
namespace N
{
    using System.IO;
    using System.Windows.Forms;

    public class Winform : Form
    {
        Winform()
        {
            var stream = File.OpenRead(string.Empty);
            // Since this is added to components, it is automatically disposed of with the form.
            this.components.Add(stream);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindVariableDeclaration("stream = File.OpenRead(string.Empty)");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
                Assert.AreEqual(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
            }
        }
    }
}
