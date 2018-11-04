namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DisposableTests
    {
        public class IsArgumentDisposedByReturnValue
        {
            [Test]
            public void CallWrappingStreamInReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public string Bar()
        {
            using (var reader = GetReader(File.OpenRead(string.Empty)))
            {
                return reader.ReadLine();
            }
        }

        private static StreamReader GetReader(Stream stream)
        {
            return new StreamReader(stream);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindArgument("File.OpenRead(string.Empty)");
                Assert.AreEqual(Result.AssumeYes, Disposable.IsArgumentDisposedByReturnValue(value, semanticModel, CancellationToken.None));
            }

            [Test]
            public void Recursive()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public string Bar()
        {
            using (var reader = GetReader(File.OpenRead(string.Empty)))
            {
                return reader.ReadLine();
            }
        }

        private static StreamReader GetReader(Stream stream)
        {
            return GetReader(stream);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindArgument("File.OpenRead(string.Empty)");
                Assert.AreEqual(Result.No, Disposable.IsArgumentDisposedByReturnValue(value, semanticModel, CancellationToken.None));
            }
        }
    }
}
