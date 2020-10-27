namespace IDisposableAnalyzers.Test
{
    using System.IO;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static class BinaryReference
    {
        public static MetadataReference Compile(string code)
        {
            var binaryReferencedCompilation = CSharpCompilation.Create(
                "BinaryReferencedAssembly",
                new[] { CSharpSyntaxTree.ParseText(code) },
                MetadataReferences.FromAttributes(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var binaryReferencedContent = new MemoryStream();
            var binaryEmitResult = binaryReferencedCompilation.Emit(binaryReferencedContent);
            Assert.That(binaryEmitResult.Diagnostics, Is.Empty);

            binaryReferencedContent.Position = 0;
            return MetadataReference.CreateFromStream(binaryReferencedContent);
        }
    }
}
