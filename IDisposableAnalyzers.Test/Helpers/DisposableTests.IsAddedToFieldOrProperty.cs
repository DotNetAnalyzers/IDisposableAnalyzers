namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DisposableTests
    {
        public class IsAddedToFieldOrProperty
        {
            [Test]
            public void WhenNotUsed()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class Foo
    {
        internal Foo(IDisposable disposable)
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var ctor = syntaxTree.FindConstructorDeclaration("internal Foo(IDisposable disposable)");
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(false, Disposable.IsAddedToFieldOrProperty(symbol, ctor, semanticModel, CancellationToken.None));
            }

            [TestCase("Add(disposable)")]
            [TestCase("Insert(1, disposable)")]
            public void WhenListOfT(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class Foo
    {
        private List<IDisposable> disposables = new List<IDisposable>();

        internal Foo(IDisposable disposable)
        {
            this.disposables.Add(disposable);
        }
    }
}".AssertReplace("Add(disposable)", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var ctor = syntaxTree.FindConstructorDeclaration("internal Foo(IDisposable disposable)");
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, Disposable.IsAddedToFieldOrProperty(symbol, ctor, semanticModel, CancellationToken.None));
            }

            [Test]
            public void WhenStackOfT()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class Foo
    {
        private Stack<IDisposable> disposables = new Stack<IDisposable>();

        internal Foo(IDisposable disposable)
        {
            this.disposables.Push(disposable);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var ctor = syntaxTree.FindConstructorDeclaration("internal Foo(IDisposable disposable)");
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, Disposable.IsAddedToFieldOrProperty(symbol, ctor, semanticModel, CancellationToken.None));
            }

            [TestCase("private Queue<IDisposable> disposables = new Queue<IDisposable>()")]
            [TestCase("private ConcurrentQueue<IDisposable> disposables = new ConcurrentQueue<IDisposable>()")]
            public void WhenQueueOfT(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    internal class Foo
    {
        private Queue<IDisposable> disposables = new Queue<IDisposable>();

        internal Foo(IDisposable disposable)
        {
            this.disposables.Enqueue(disposable);
        }
    }
}".AssertReplace("private Queue<IDisposable> disposables = new Queue<IDisposable>()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var ctor = syntaxTree.FindConstructorDeclaration("internal Foo(IDisposable disposable)");
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, Disposable.IsAddedToFieldOrProperty(symbol, ctor, semanticModel, CancellationToken.None));
            }

            [TestCase("private Dictionary<int, IDisposable> disposables = new Dictionary<int, IDisposable>()")]
            [TestCase("private IDictionary<int, IDisposable> disposables = new Dictionary<int, IDisposable>()")]
            [TestCase("private IDictionary disposables = new Dictionary<int, IDisposable>()")]
            public void WhenDictionaryAdd(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class Foo
    {
        private Dictionary<int, IDisposable> disposables = new Dictionary<int, IDisposable>();

        internal Foo(IDisposable disposable)
        {
            this.disposables.Add(1, disposable);
        }
    }
}".AssertReplace("private Dictionary<int, IDisposable> disposables = new Dictionary<int, IDisposable>()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var ctor = syntaxTree.FindConstructorDeclaration("internal Foo(IDisposable disposable)");
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, Disposable.IsAddedToFieldOrProperty(symbol, ctor, semanticModel, CancellationToken.None));
            }

            [TestCase("TryAdd(1, disposable)")]
            [TestCase("TryUpdate(1, disposable, disposable)")]
            public void WhenConcurrentDictionary(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Concurrent;

    internal class Foo
    {
        private ConcurrentDictionary<int, IDisposable> disposables = new ConcurrentDictionary<int, IDisposable>();

        internal Foo(IDisposable disposable)
        {
            this.disposables.TryAdd(1, disposable);
        }
    }
}".AssertReplace("TryAdd(1, disposable)", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var ctor = syntaxTree.FindConstructorDeclaration("internal Foo(IDisposable disposable)");
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, Disposable.IsAddedToFieldOrProperty(symbol, ctor, semanticModel, CancellationToken.None));
            }

            [Test]
            public void WhenAddListOfTInitialize()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class Foo
    {
        private List<IDisposable> disposables = new List<IDisposable>();

        internal Foo(IDisposable disposable)
        {
            this.Initialize(disposable);
        }

        private void Initialize(IDisposable disposable)
        {
            this.disposables.Add(disposable);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var ctor = syntaxTree.FindConstructorDeclaration("internal Foo(IDisposable disposable)");
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, Disposable.IsAddedToFieldOrProperty(symbol, ctor, semanticModel, CancellationToken.None));
            }
        }
    }
}
