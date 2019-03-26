namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(false, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out _));
            }

            [TestCase("string", "string.Format(\"{0}\", disposable)")]
            [TestCase("string", "disposable.ToString()")]
            [TestCase("bool", "disposable is null")]
            [TestCase("bool", "disposable == null")]
            [TestCase("bool", "Equals(disposable, null)")]
            [TestCase("bool", "this.Equals(disposable)")]
            [TestCase("bool", "object.Equals(disposable, null)")]
            public void WhenNotUsed(string type, string expression)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class C
    {
        private readonly bool value;

        internal C(IDisposable disposable)
        {
            this.value = Equals(disposable, null);
        }
    }
}".AssertReplace("bool", type)
  .AssertReplace("Equals(disposable, null)", expression);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(false, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out _));
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
            }

            [TestCase("Initialize(disposable)")]
            [TestCase("this.Initialize(disposable)")]
            public void ListOfTAddInInitialize(string call)
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
}".AssertReplace("this.Initialize(disposable)", call);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
            }

            [TestCase("Initialize(disposables, disposable)")]
            [TestCase("this.Initialize(this.disposables, disposable)")]
            public void ListOfTAddInInitializePassField(string call)
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
            this.Initialize(this.disposables, disposable);
        }

        private void Initialize(List<IDisposable> disposables, IDisposable disposable)
        {
            disposables.Add(disposable);
        }
    }
}".AssertReplace("this.Initialize(this.disposables, disposable)", call);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual(SymbolKind.Parameter, container.Kind);
                Assert.AreEqual("disposables", container.Name);
            }

            [TestCase("Initialize(disposables, disposable)")]
            [TestCase("this.Initialize(disposables, disposable)")]
            public void ListOfTAddInInitializeParameter(string call)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        internal C(List<IDisposable> disposables, IDisposable disposable)
        {
            this.Initialize(disposables, disposable);
        }

        private void Initialize(List<IDisposable> disposables, IDisposable disposable)
        {
            disposables.Add(disposable);
        }
    }
}".AssertReplace("this.Initialize(disposables, disposable)", call);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("IDisposable disposable");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual(SymbolKind.Parameter, container.Kind);
                Assert.AreEqual("disposables", container.Name);
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposables", container.ToString());
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.tuple", container.ToString());
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
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(false, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out _));
            }

            [TestCase("disposable1")]
            [TestCase("disposable2")]
            public void InPairWhenNew(string parameter)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal class C
    {
        private readonly Pair<IDisposable> pair;

        internal C(IDisposable disposable1, IDisposable disposable2)
        {
            this.pair = new Pair<IDisposable>(disposable1, disposable2);
        }

        public class Pair<T>
        {
            public Pair(T item1, T item2)
            {
                this.Item1 = item1;
                this.Item2 = item2;
            }

            public T Item1 { get; }

            public T Item2 { get; }
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter(parameter);
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.pair", container.ToString());
            }

            [TestCase("disposable1")]
            [TestCase("disposable2")]
            public void InPairWhenFactoryMethod(string parameter)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class C
    {
        private readonly Pair<IDisposable> pair;

        internal C(IDisposable disposable1, IDisposable disposable2)
        {
            this.pair = Create<IDisposable>(disposable1, disposable2);
        }

        public static Pair<T> Create<T>(T x, T y) => new Pair<T>(x, y);

        public class Pair<T>
        {
            public Pair(T item1, T item2)
            {
                this.Item1 = item1;
                this.Item2 = item2;
            }

            public T Item1 { get; }

            public T Item2 { get; }
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter(parameter);
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.pair", container.ToString());
            }

            [TestCase("disposable1")]
            [TestCase("disposable2")]
            public void InDisposingPairWhenNew(string parameter)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class C
    {
        private readonly Pair<IDisposable> pair;

        internal C(IDisposable disposable1, IDisposable disposable2)
        {
            this.pair = new Pair<IDisposable>(disposable1, disposable2);
        }

        private class Pair<T> : IDisposable
            where T : IDisposable
        {
            private readonly T item1;
            private readonly T item2;

            public Pair(T item1, T item2)
            {
                this.item1 = item1;
                this.item2 = item2;
            }

            public void Dispose()
            {
                this.item1.Dispose();
                (this.item2 as IDisposable)?.Dispose();
            }
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter(parameter);
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.pair", container.ToString());
            }

            [TestCase("disposable1")]
            [TestCase("disposable2")]
            public void InDisposingPairWhenFactoryMethod(string parameter)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class C
    {
        private readonly Pair<IDisposable> pair;

        internal C(IDisposable disposable1, IDisposable disposable2)
        {
            this.pair = Create<IDisposable>(disposable1, disposable2);
        }

        private static Pair<T> Create<T>(T x, T y) where T : IDisposable => new Pair<T>(x, y);

        private class Pair<T> : IDisposable
            where T : IDisposable
        {
            private readonly T item1;
            private readonly T item2;

            public Pair(T item1, T item2)
            {
                this.item1 = item1;
                this.item2 = item2;
            }

            public void Dispose()
            {
                this.item1.Dispose();
                (this.item2 as IDisposable)?.Dispose();
            }
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter(parameter);
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.pair", container.ToString());
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
            public void InLeaveOpen(string expression, bool stores)
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("stream");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(stores, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual(stores, DisposableWalker.DisposedByReturnValue(syntaxTree.FindArgument("stream"), semanticModel, CancellationToken.None, null));
                if (stores)
                {
                    Assert.AreEqual("RoslynSandbox.C.disposable", container.ToString());
                }
            }

            [TestCase("new HttpClient(handler)", true)]
            [TestCase("new HttpClient(handler, disposeHandler: true)", true)]
            [TestCase("new HttpClient(handler, disposeHandler: false)", false)]
            public void InHttpClient(string expression, bool stores)
            {
                var testCode = @"
namespace RoslynSandbox
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
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("handler");
                Assert.AreEqual(true,   semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true,   LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(stores, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual(stores, DisposableWalker.DisposedByReturnValue(syntaxTree.FindArgument("handler"), semanticModel, CancellationToken.None, null));
                if (stores)
                {
                    Assert.AreEqual("RoslynSandbox.C.disposable", container.ToString());
                }
            }

            [Test]
            public void CallWrappingStreamInReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        private readonly IDisposable disposable;

        public C(Stream stream)
        {
            this.disposable = GetReader(stream);
        }

        private static StreamReader GetReader(Stream arg)
        {
            return new StreamReader(arg);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("Stream stream");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("RoslynSandbox.C.disposable", container.ToString());
            }

            [Test]
            public void DisposedByReturnValueCallWrappingStreamInReader()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public string M()
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
                Assert.AreEqual(true, DisposableWalker.DisposedByReturnValue(value, semanticModel, CancellationToken.None, null));
            }

            [Test]
            public void Recursive()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public C(Stream stream)
        {
            this.disposable = GetReader(stream);
        }

        private static StreamReader GetReader(Stream arg)
        {
            return GetReader(arg);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("Stream stream");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(false, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out _));
            }

            [TestCase("disposable.AddAndReturn(stream)")]
            [TestCase("disposable.AddAndReturn(stream).ToString()")]
            public void CompositeDisposableExtAddAndReturn(string expression)
            {
                var code = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public static class CompositeDisposableExt
    {
        public static T AddAndReturn<T>(this CompositeDisposable disposable, T item)
            where T : IDisposable
        {
            if (item != null)
            {
                disposable.Add(item);
            }

            return item;
        }
    }

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public void Dispose()
        {
            this.disposable.Dispose();
        }

        internal object M(Stream stream)
        {
            return this.disposable.AddAndReturn(stream);
        }
    }
}".AssertReplace("disposable.AddAndReturn(stream)", expression);

                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindParameter("Stream stream");
                Assert.AreEqual(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
                Assert.AreEqual(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
                Assert.AreEqual(true, DisposableWalker.Stores(localOrParameter, semanticModel, CancellationToken.None, null, out var container));
                Assert.AreEqual("disposable", container.Name);
                Assert.AreEqual(SymbolKind.Parameter, container.Kind);
            }
        }
    }
}
