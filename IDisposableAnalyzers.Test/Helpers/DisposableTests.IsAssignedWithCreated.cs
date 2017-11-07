namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal partial class DisposableTests
    {
        internal class IsAssignedWithCreated
        {
            [Test]
            public void LambdaInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Bar += (o, e) => this.stream = File.OpenRead(string.Empty);
        }

        public event EventHandler Bar;
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var assignment = syntaxTree.FindInvocation("File.OpenRead(string.Empty)").FirstAncestor<AssignmentExpressionSyntax>();
                Assert.AreEqual(Result.Yes, Disposable.IsAssignedWithCreated(assignment.Left, semanticModel, CancellationToken.None, out var assignedSymbol));
                Assert.AreEqual("stream", assignedSymbol.Name);
            }
        }
    }
}