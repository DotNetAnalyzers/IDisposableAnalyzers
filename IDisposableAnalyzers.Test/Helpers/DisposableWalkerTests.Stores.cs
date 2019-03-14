namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DisposableWalkerTests
    {
        public class Stores
        {
            [Test]
            public void WhenNotUsed()
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
                Assert.AreEqual(false, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [TestCase("Add(disposable)")]
            [TestCase("Insert(1, disposable)")]
            public void InListOfTAdd(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        private List<IDisposable> disposables = new List<IDisposable>();

        internal C(IDisposable disposable)
        {
            this.disposables.Add(disposable);
        }
    }
}".AssertReplace("Add(disposable)", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true,  LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void ListOfTAddInInitialize()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        private List<IDisposable> disposables = new List<IDisposable>();

        internal C(IDisposable disposable)
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
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void ListOfTAssignIndexer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        private List<IDisposable> disposables = new List<IDisposable> { null };

        internal C(IDisposable disposable)
        {
            this.disposables[0] = disposable;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void ListOfTInitializer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        private List<IDisposable> disposables;

        internal C(IDisposable disposable)
        {
            this.disposables = new List<IDisposable> { disposable };
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [TestCase("new Disposable[] { disposable }")]
            [TestCase("new[] { disposable }")]
            public void ArrayOfTInitializer(string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        private IDisposable[] disposables;

        internal C(IDisposable disposable)
        {
            this.disposables =  new Disposable[] { disposable };
        }
    }
}".AssertReplace("new Disposable[] { disposable }", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void InStackOfT()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        private Stack<IDisposable> disposables = new Stack<IDisposable>();

        internal C(IDisposable disposable)
        {
            this.disposables.Push(disposable);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [TestCase("private Queue<IDisposable> disposables = new Queue<IDisposable>()")]
            [TestCase("private ConcurrentQueue<IDisposable> disposables = new ConcurrentQueue<IDisposable>()")]
            public void InQueueOfT(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    internal class C
    {
        private Queue<IDisposable> disposables = new Queue<IDisposable>();

        internal C(IDisposable disposable)
        {
            this.disposables.Enqueue(disposable);
        }
    }
}".AssertReplace("private Queue<IDisposable> disposables = new Queue<IDisposable>()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [TestCase("private Dictionary<int, IDisposable> disposables = new Dictionary<int, IDisposable>()")]
            [TestCase("private IDictionary<int, IDisposable> disposables = new Dictionary<int, IDisposable>()")]
            [TestCase("private IDictionary disposables = new Dictionary<int, IDisposable>()")]
            public void InDictionaryAdd(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class C
    {
        private Dictionary<int, IDisposable> disposables = new Dictionary<int, IDisposable>();

        internal C(IDisposable disposable)
        {
            this.disposables.Add(1, disposable);
        }
    }
}".AssertReplace("private Dictionary<int, IDisposable> disposables = new Dictionary<int, IDisposable>()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [TestCase("TryAdd(1, disposable)")]
            [TestCase("TryUpdate(1, disposable, disposable)")]
            public void InConcurrentDictionary(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Concurrent;

    internal class C
    {
        private ConcurrentDictionary<int, IDisposable> disposables = new ConcurrentDictionary<int, IDisposable>();

        internal C(IDisposable disposable)
        {
            this.disposables.TryAdd(1, disposable);
        }
    }
}".AssertReplace("TryAdd(1, disposable)", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void ArrayFieldAssignedInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class C
    {
        private IDisposable[] disposables = new IDisposable[1];

        internal C(IDisposable disposable)
        {
            this.disposables[0] = disposable;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [TestCase("Tuple.Create(disposable, 1)")]
            [TestCase("new Tuple<IDisposable, int>(disposable, 1)")]
            public void InTuple(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        private readonly Tuple<IDisposable, int> tuple;

        internal C(IDisposable disposable)
        {
            this.tuple = Tuple.Create(disposable, 1);
        }
    }
}".AssertReplace("Tuple.Create(disposable, 1)", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }

            [TestCase("_ = Tuple.Create(disposable, 1)")]
            [TestCase("Tuple.Create(disposable, 1)")]
            [TestCase("new Tuple<IDisposable, int>(disposable, 1)")]
            public void InDiscardedTuple(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        internal C(IDisposable disposable)
        {
            _ = Tuple.Create(disposable, 1);
        }
    }
}".AssertReplace("Tuple.Create(disposable, 1)", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(false, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null));
            }
        }
    }
}
