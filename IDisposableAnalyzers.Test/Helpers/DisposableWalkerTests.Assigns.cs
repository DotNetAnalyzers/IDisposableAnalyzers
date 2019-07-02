namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public static partial class DisposableWalkerTests
    {
        public static class Assigns
        {
            [Test]
            public static void WhenNotUsed()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(false, DisposableWalker.Assigns(localOrParameter, semanticModel, CancellationToken.None, null, out _));
            }

            [Test]
            public static void AssigningLocal()
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(false, DisposableWalker.Assigns(localOrParameter, semanticModel, CancellationToken.None, null, out _));
            }

            [Test]
            public static void FieldAssignedInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Assigns(localOrParameter, semanticModel, CancellationToken.None, null, out var field));
                Assert.AreEqual("RoslynSandbox.C.disposable", field.Symbol.ToString());
            }

            [Test]
            public static void FieldAssignedViaCalledMethodParameter()
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Assigns(localOrParameter, semanticModel, CancellationToken.None, null, out var field));
                Assert.AreEqual("RoslynSandbox.C.disposable", field.Symbol.ToString());
            }

            [Test]
            public static void FieldAssignedInCtorViaLocal()
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Assigns(localOrParameter, semanticModel, CancellationToken.None, null, out var field));
                Assert.AreEqual("RoslynSandbox.C.disposable", field.Symbol.ToString());
            }

            [Test]
            public static void PropertyAssignedInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Assigns(localOrParameter, semanticModel, CancellationToken.None, null, out var field));
                Assert.AreEqual("RoslynSandbox.C.Disposable", field.Symbol.ToString());
            }

            [Test]
            public static void PropertyAssignedInCalledMethod()
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Assigns(localOrParameter, semanticModel, CancellationToken.None, null, out var field));
                Assert.AreEqual("RoslynSandbox.C.Disposable", field.Symbol.ToString());
            }
        }
    }
}
