namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DisposableTests
    {
        public class IsIgnored
        {
            [Test]
            public void AssignedToLocal()
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class Foo
    {
        internal Foo(string fileName)
        {
            var value = File.OpenRead(fileName);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation =
                    CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(false, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }

            [TestCase("_ = File.OpenRead(fileName)")]
            [TestCase("File.OpenRead(fileName)")]
            [TestCase("var _ = File.OpenRead(fileName)")]
            public void Discarded(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal class Foo
    {
        internal Foo(string fileName)
        {
            _ = File.OpenRead(fileName);
        }
    }
}".AssertReplace("_ = File.OpenRead(fileName)", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, Disposable.IsIgnored(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
