namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    public static class DisposableMemberTests
    {
        [Test]
        public static void SimpleField()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using System.IO;

    class C : IDisposable
    {
        private readonly IDisposable stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var field = syntaxTree.FindFieldDeclaration("stream");
            var fieldSymbol = semanticModel.GetDeclaredSymbolSafe(field, CancellationToken.None);
            Assert.AreEqual(Result.Yes, DisposableMember.IsDisposed(new FieldOrProperty(fieldSymbol), (TypeDeclarationSyntax)field.Parent, semanticModel, CancellationToken.None));
        }
    }
}
