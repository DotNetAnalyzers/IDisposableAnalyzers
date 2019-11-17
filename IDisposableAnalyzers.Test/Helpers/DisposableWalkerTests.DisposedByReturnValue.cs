namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class DisposableWalkerTests
    {
        public static class DisposedByReturnValue
        {
            [Test]
            public static void FactoryMethod()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class Disposer : IDisposable
    {
        private readonly Stream stream;

        private Disposer(Stream stream)
        {
            this.stream = stream;
        }

        public static Disposer M() => new Disposer(File.OpenRead(string.Empty));

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindArgument("File.OpenRead(string.Empty)");
                Assert.AreEqual(true, DisposableWalker.DisposedByReturnValue(value, semanticModel, CancellationToken.None, out _));
            }
        }
    }
}
