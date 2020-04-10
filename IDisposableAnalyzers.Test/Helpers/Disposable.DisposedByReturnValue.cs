namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class DisposableTests
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
                Assert.AreEqual(true, Disposable.DisposedByReturnValue(value, semanticModel, CancellationToken.None, out _));
            }

            [TestCase("new BinaryReader(stream)", true)]
            [TestCase("new BinaryReader(stream, new UTF8Encoding(), true)", false)]
            [TestCase("new BinaryReader(stream, new UTF8Encoding(), leaveOpen: true)", false)]
            [TestCase("new BinaryReader(stream, encoding: new UTF8Encoding(), leaveOpen: true)", false)]
            [TestCase("new BinaryReader(stream, leaveOpen: true, encoding: new UTF8Encoding())", false)]
            [TestCase("new BinaryReader(stream, new UTF8Encoding(), false)", true)]
            [TestCase("new BinaryReader(stream, leaveOpen: false, encoding: new UTF8Encoding())", true)]
            [TestCase("new BinaryWriter(stream, new UTF8Encoding(), leaveOpen: false)", true)]
            [TestCase("new BinaryWriter(stream, new UTF8Encoding(), leaveOpen: true)", false)]
            [TestCase("new StreamReader(stream, new UTF8Encoding(), true, 1024, leaveOpen: false)", true)]
            [TestCase("new StreamReader(stream, new UTF8Encoding(), true, 1024, leaveOpen: true)", false)]
            [TestCase("new StreamWriter(stream, new UTF8Encoding(), 1024, leaveOpen: false)", true)]
            [TestCase("new StreamWriter(stream, new UTF8Encoding(), 1024, leaveOpen: true)", false)]
            [TestCase("new CryptoStream(stream, new FromBase64Transform(), CryptoStreamMode.Read, leaveOpen: true)", false)]
            [TestCase("new CryptoStream(stream, new FromBase64Transform(), CryptoStreamMode.Read, leaveOpen: false)", true)]
            [TestCase("new DeflateStream(stream, CompressionLevel.Fastest)", true)]
            [TestCase("new DeflateStream(stream, CompressionLevel.Fastest, leaveOpen: true)", false)]
            [TestCase("new DeflateStream(stream, CompressionLevel.Fastest, leaveOpen: false)", true)]
            [TestCase("new GZipStream(stream, CompressionLevel.Fastest)", true)]
            [TestCase("new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: true)", false)]
            [TestCase("new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: false)", true)]
            [TestCase("new System.Net.Mail.Attachment(stream, string.Empty)", true)]
            public static void InLeaveOpen(string expression, bool stores)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using System.Text;

    public class C
    {
        private readonly IDisposable disposable;

        public C(Stream stream)
        {
            this.disposable = new BinaryReader(stream);
        }
    }
}".AssertReplace("new BinaryReader(stream)", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("stream");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(stores, Disposable.Stores(localOrParameter, semanticModel, CancellationToken.None, out var container));
                Assert.AreEqual(stores, Disposable.DisposedByReturnValue(syntaxTree.FindArgument("stream"), semanticModel, CancellationToken.None, out _));
                if (stores)
                {
                    Assert.AreEqual("N.C.disposable", container.ToString());
                }
            }

            [TestCase("new HttpClient(handler)", true)]
            [TestCase("new HttpClient(handler, disposeHandler: true)", true)]
            [TestCase("new HttpClient(handler, disposeHandler: false)", false)]
            public static void InHttpClient(string expression, bool stores)
            {
                var code = @"
namespace N
{
    using System.Net.Http;

    public class C
    {
        private readonly IDisposable disposable;

        public C(HttpClientHandler handler)
        {
            this.disposable = new HttpClient(handler);
        }
    }
}".AssertReplace("new HttpClient(handler)", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("handler");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(stores, Disposable.Stores(localOrParameter, semanticModel, CancellationToken.None, out var container));
                Assert.AreEqual(stores, Disposable.DisposedByReturnValue(syntaxTree.FindArgument("handler"), semanticModel, CancellationToken.None, out _));
                if (stores)
                {
                    Assert.AreEqual("N.C.disposable", container.ToString());
                }
            }
        }
    }
}
