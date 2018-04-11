// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class HappyPathWithAll
    {
        private static readonly ImmutableArray<DiagnosticAnalyzer> AllAnalyzers = typeof(KnownSymbol)
            .Assembly
            .GetTypes()
            .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
            .ToImmutableArray();

        private static readonly Solution Solution = CodeFactory.CreateSolution(
            CodeFactory.FindSolutionFile("IDisposableAnalyzers.sln"),
            AllAnalyzers,
            AnalyzerAssert.MetadataReferences);

        // ReSharper disable once InconsistentNaming
        private static readonly Solution IDisposableAnalyzersAnalyzersProjectSln = CodeFactory.CreateSolution(
            CodeFactory.FindProjectFile("IDisposableAnalyzers.Analyzers.csproj"),
            AllAnalyzers,
            AnalyzerAssert.MetadataReferences);

        [Test]
        public void NotEmpty()
        {
            CollectionAssert.IsNotEmpty(AllAnalyzers);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void IDisposableAnalyzersSln(DiagnosticAnalyzer analyzer)
        {
            AnalyzerAssert.Valid(analyzer, Solution);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void IDisposableAnalyzersProject(DiagnosticAnalyzer analyzer)
        {
            AnalyzerAssert.Valid(analyzer, IDisposableAnalyzersAnalyzersProjectSln);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void SomewhatRealisticSample(DiagnosticAnalyzer analyzer)
        {
            var disposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable(string meh)
            : this()
        {
        }

        public Disposable()
        {
        }

        public void Dispose()
        {
        }
    }
}";

            var fooListCode = @"
namespace RoslynSandbox
{
    using System.Collections;
    using System.Collections.Generic;

    internal class FooList<T> : IReadOnlyList<T>
    {
        private readonly List<T> inner = new List<T>();

        public int Count => this.inner.Count;

        public T this[int index] => this.inner[index];

        public IEnumerator<T> GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.inner).GetEnumerator();
        }
    }
}";

            var foo1Code = @"
namespace RoslynSandbox
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Reactive.Disposables;

    public class Foo1 : IDisposable
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();
        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();

        private IDisposable meh1;
        private IDisposable meh2;
        private bool isDirty;

        public Foo()
        {
            this.meh1 = this.RecursiveProperty;
            this.meh2 = this.RecursiveMethod();
            this.subscription.Disposable = File.OpenRead(string.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { this.PropertyChangedCore += value; }
            remove { this.PropertyChangedCore -= value; }
        }

        private event PropertyChangedEventHandler PropertyChangedCore;

        public Disposable RecursiveProperty => RecursiveProperty;

        public IDisposable Disposable => subscription.Disposable;

        public bool IsDirty
        {
            get
            {
                return this.isDirty;
            }

            private set
            {
                if (value == this.isDirty)
                {
                    return;
                }

                this.isDirty = value;
                this.PropertyChangedCore?.Invoke(this, IsDirtyPropertyChangedEventArgs);
            }
        }

        public Disposable RecursiveMethod() => RecursiveMethod();

        public void Meh()
        {
            using (var item = new Disposable())
            {
            }

            using (var item = RecursiveProperty)
            {
            }

            using (RecursiveProperty)
            {
            }

            using (var item = RecursiveMethod())
            {
            }

            using (RecursiveMethod())
            {
            }
        }

        public void Dispose()
        {
            this.subscription.Dispose();
            this.compositeDisposable.Dispose();
        }

        internal string AddAndReturnToString()
        {
            return this.compositeDisposable.AddAndReturn(new Disposable()).ToString();
        }
    }
}";

            var fooBaseCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public abstract class FooBase : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed = false;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (disposing)
            {
                this.stream.Dispose();
            }
        }
    }
}";

            var fooImplCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class FooImpl : FooBase
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";

            var foo2Code = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo2
    {
        private IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = Bar(disposable);
        }

        private static IDisposable Bar(IDisposable disposable, IEnumerable<IDisposable> disposables = null)
        {
            if (disposables == null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }
    }
}";

            var reactiveCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public abstract class RxFoo : IDisposable
    {
        private readonly IDisposable subscription;
        private readonly SingleAssignmentDisposable singleAssignmentDisposable = new SingleAssignmentDisposable();

        public RxFoo(int no)
            : this(Create(no))
        {
        }

        public RxFoo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ => { });
            this.singleAssignmentDisposable.Disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
            this.singleAssignmentDisposable.Dispose();
        }

        private static IObservable<object> Create(int i)
        {
            return Observable.Empty<object>();
        }
     }
}";

            var lazyCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class LazyFoo : IDisposable
    {
        private readonly IDisposable created;
        private bool disposed;
        private IDisposable lazyDisposable;

        public LazyFoo(IDisposable injected)
        {
            this.Disposable = injected ?? (this.created = new Disposable());
        }

        public IDisposable Disposable { get; }

        public IDisposable LazyDisposable => this.lazyDisposable ?? (this.lazyDisposable = new Disposable());

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.created?.Dispose();
            this.lazyDisposable?.Dispose();
        }
    }
}";
            var asyncCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public static class FooAsync
    {
        public static async Task<string> Bar1Async()
        {
            using (var stream = await ReadAsync(string.Empty))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadLine();
                }
            }
        }

        public static async Task<string> Bar2Async()
        {
            using (var stream = await ReadAsync(string.Empty).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadLine();
                }
            }
        }

        private static async Task<Stream> ReadAsync(this string fileName)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(fileName))
            {
                await fileStream.CopyToAsync(stream)
                    .ConfigureAwait(false);
            }

            stream.Position = 0;
            return stream;
        }
    }
}";
            var compositeDisposableExtCode = @"
namespace RoslynSandbox
{
    using System;
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
}";

            var foo3Code = @"
namespace RoslynSandbox
{
    using System.IO;

    public class FooOut
    {
        public static bool TryGetStream(out Stream stream)
        {
            return TryGetStreamCore(out stream);
        }

        public void Bar()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }

        public void Baz()
        {
            IDisposable disposable;
            if (TryGet(out disposable))
            {
                using (disposable)
                {
                }
            }
        }

        private static bool TryGetStreamCore(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
            var sources = new[]
                          {
                              disposableCode,
                              fooListCode,
                              foo1Code,
                              foo2Code,
                              foo3Code,
                              fooBaseCode,
                              fooImplCode,
                              reactiveCode,
                              lazyCode,
                              asyncCode,
                              compositeDisposableExtCode,
                          };
            AnalyzerAssert.Valid(analyzer, sources);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void ReactiveSample(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public abstract class RxFoo : IDisposable
    {
        private readonly IDisposable subscription;
        private readonly SingleAssignmentDisposable singleAssignmentDisposable = new SingleAssignmentDisposable();

        public RxFoo(int no)
            : this(Create(no))
        {
        }

        public RxFoo(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ => { });
            this.singleAssignmentDisposable.Disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
            this.singleAssignmentDisposable.Dispose();
        }

        private static IObservable<object> Create(int i)
        {
            return Observable.Empty<object>();
        }
     }
}";
            AnalyzerAssert.Valid(analyzer, testCode);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void WithSyntaxErrors(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo : SyntaxError
    {
        private readonly Stream stream = File.SyntaxError(string.Empty);
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.syntaxError)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }";
            AnalyzerAssert.Valid(analyzer, testCode);
        }
    }
}
