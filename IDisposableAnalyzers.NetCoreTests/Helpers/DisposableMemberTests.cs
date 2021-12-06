namespace IDisposableAnalyzers.NetCoreTests.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static class DisposableMemberTests
    {
        [Test]
        public static void SimpleFieldIAsyncDisposable()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync();
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var declaration = syntaxTree.FindFieldDeclaration("disposable");
            var symbol = semanticModel.GetDeclaredSymbolSafe(declaration, CancellationToken.None);
            Assert.AreEqual(true, DisposableMember.IsDisposed(new FieldOrPropertyAndDeclaration(symbol!, declaration), semanticModel, CancellationToken.None));
        }
    }
}
