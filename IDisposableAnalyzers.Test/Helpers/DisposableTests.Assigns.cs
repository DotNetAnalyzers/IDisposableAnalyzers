namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class DisposableTests
    {
        public static class Assigns
        {
            [Test]
            public static void WhenNotUsed()
            {
                var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(false, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public static void AssigningLocal()
            {
                var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
            var temp = disposable;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(false, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public static void FieldAssignedInCtor()
            {
                var code = @"
namespace N
{
    using System;

    internal class C
    {
        private IDisposable disposable;

        internal C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
                Assert.AreEqual("N.C.disposable", field.Symbol.ToString());
            }

            [Test]
            public static void FieldAssignedViaCalledMethodParameter()
            {
                var code = @"
namespace N
{
    using System;

    internal class C
    {
        private IDisposable disposable;

        internal C(IDisposable disposable)
        {
            this.M(disposable);
        }

        private void M(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
                Assert.AreEqual("N.C.disposable", field.Symbol.ToString());
            }

            [Test]
            public static void FieldAssignedInCtorViaLocal()
            {
                var code = @"
namespace N
{
    using System;

    internal class C
    {
        private IDisposable disposable;

        internal C(IDisposable disposable)
        {
            var temp = disposable;
            this.disposable = temp;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
                Assert.AreEqual("N.C.disposable", field.Symbol.ToString());
            }

            [Test]
            public static void PropertyAssignedInCtor()
            {
                var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
                Assert.AreEqual("N.C.Disposable", field.Symbol.ToString());
            }

            [Test]
            public static void PropertyAssignedInCalledMethod()
            {
                var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
            this.M(disposable);
        }

        public IDisposable Disposable { get; private set; }

        private void M(IDisposable arg)
        {
            this.Disposable = arg;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
                Assert.AreEqual("N.C.Disposable", field.Symbol.ToString());
            }

            [Test]
            public static void PropertyAssignedViaIdentity()
            {
                var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
            this.Disposable = this.M(disposable);
        }

        public IDisposable Disposable { get; private set; }

        private void M(IDisposable arg) => arg;
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
                Assert.AreEqual("N.C.Disposable", field.Symbol.ToString());
            }

            [TestCase("Task.FromResult(File.OpenRead(fileName))")]
            [TestCase("Task.FromResult(File.OpenRead(fileName)).ConfigureAwait(true)")]
            [TestCase("Task.Run(() => File.OpenRead(fileName))")]
            [TestCase("Task.Run(() => { return File.OpenRead(fileName); })")]
            [TestCase("Task.Run(() => File.OpenRead(fileName)).ConfigureAwait(true)")]
            public static void AssigningFieldAwait(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public sealed class C : IDisposable
    {
        private IDisposable disposable;

        public async Task M(string fileName)
        {
            this.disposable?.Dispose();
            this.disposable = await Task.FromResult(File.OpenRead(fileName));
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}".AssertReplace("Task.FromResult(File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, Disposable.Assigns(value, semanticModel, CancellationToken.None, out var fieldOrProperty));
                Assert.AreEqual("disposable", fieldOrProperty.Name);
            }

            [TestCase("Task.FromResult(File.OpenRead(fileName)).Result")]
            [TestCase("Task.FromResult(File.OpenRead(fileName)).GetAwaiter().GetResult()")]
            [TestCase("Task.Run(() => File.OpenRead(fileName)).Result")]
            [TestCase("Task.Run(() => File.OpenRead(fileName)).GetAwaiter().GetResult()")]
            [TestCase("Task.Run(() => { return File.OpenRead(fileName); }).Result")]
            [TestCase("Task.Run(() => { return File.OpenRead(fileName); }).GetAwaiter().GetResult()")]
            public static void AssigningFieldGetAwaiterGetResult(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public sealed class C : IDisposable
    {
        private IDisposable disposable;

        public async Task M(string fileName)
        {
            this.disposable?.Dispose();
            this.disposable = Task.FromResult(File.OpenRead(fileName));
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}".AssertReplace("Task.FromResult(File.OpenRead(fileName))", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
                Assert.AreEqual(true, Disposable.Assigns(value, semanticModel, CancellationToken.None, out var fieldOrProperty));
                Assert.AreEqual("disposable", fieldOrProperty.Name);
            }
        }
    }
}
