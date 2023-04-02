namespace IDisposableAnalyzers.Test.Helpers;

using System.Threading;
using Gu.Roslyn.AnalyzerExtensions;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;
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
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var declaration = syntaxTree.FindFieldDeclaration("stream");
        var symbol = semanticModel.GetDeclaredSymbolSafe(declaration, CancellationToken.None);
        Assert.AreEqual(true, DisposableMember.IsDisposed(new FieldOrPropertyAndDeclaration(symbol, declaration), semanticModel, CancellationToken.None));
    }

    [Test]
    public static void DisposedInOverridden()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using System.IO;

    abstract class BaseClass : IDisposable
    {
        public void Dispose()
        {
            this.M();
        }

        protected abstract void M();
    }

    class C : BaseClass
    {
        private readonly IDisposable stream = File.OpenRead(string.Empty);

        protected override void M()
        {
            this.stream.Dispose();
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var declaration = syntaxTree.FindFieldDeclaration("stream");
        var symbol = semanticModel.GetDeclaredSymbolSafe(declaration, CancellationToken.None);
        Assert.AreEqual(true, DisposableMember.IsDisposed(new FieldOrPropertyAndDeclaration(symbol, declaration), semanticModel, CancellationToken.None));
    }

    [Test]
    public static void DisposedInExplicitImplementation()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using System.IO;

    sealed class C : IDisposable
    {
        private readonly IDisposable stream = File.OpenRead(string.Empty);

        void IDisposable.Dispose()
        {
            this.stream.Dispose();
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var declaration = syntaxTree.FindFieldDeclaration("stream");
        var symbol = semanticModel.GetDeclaredSymbolSafe(declaration, CancellationToken.None);
        var field = new FieldOrPropertyAndDeclaration(symbol, declaration);
        Assert.AreEqual(true, DisposableMember.IsDisposed(field, semanticModel, CancellationToken.None));
    }

    [TestCase("this.components.Add(this.stream)")]
    [TestCase("components.Add(stream)")]
    public static void FieldAddedToFormComponents(string expression)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.IO;
    using System.Windows.Forms;

    public class Winform : Form
    {
        private readonly Stream stream;

        Winform()
        {
            this.stream = File.OpenRead(string.Empty);
            // Since this is added to components, it is automatically disposed of with the form.
            this.components.Add(this.stream);
        }
    }
}".AssertReplace("this.components.Add(this.stream)", expression));
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var declaration = syntaxTree.FindFieldDeclaration("stream");
        var symbol = semanticModel.GetDeclaredSymbolSafe(declaration, CancellationToken.None);
        Assert.AreEqual(true, DisposableMember.IsDisposed(new FieldOrPropertyAndDeclaration(symbol, declaration), semanticModel, CancellationToken.None));
    }
}
