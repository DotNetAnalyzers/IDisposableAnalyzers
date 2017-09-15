namespace IDisposableAnalyzers.Test
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class HappyPathWithAll
    {
        private static readonly ImmutableArray<DiagnosticAnalyzer> AllAnalyzers = typeof(KnownSymbol)
            .Assembly
            .GetTypes()
            .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
            .Select(t => (DiagnosticAnalyzer) Activator.CreateInstance(t))
            .ToImmutableArray();

        [TestCaseSource(nameof(AllAnalyzers))]
        public void SomewhatRealisticSample(DiagnosticAnalyzer analyzer)
        {
            var disposableCode = @"
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
}";

            var fooListCode = @"
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
}";

            var fooCode = @"
using System;
using System.ComponentModel;
using System.IO;
using System.Reactive.Disposables;

public class Foo : IDisposable
{
    private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
    private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

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
    }
}";

            var fooBaseCode = @"
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
    }";

            var fooImplCode = @"
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
    }";

            var withOptionalParameterCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
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

            var sources = new[] { disposableCode, fooListCode, fooCode, fooBaseCode, fooImplCode, withOptionalParameterCode, reactiveCode };
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
        public void RecursiveSample(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public abstract class Foo
    {
        public Foo()
        {
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(1);
            // value = value;
        }

        public int RecursiveExpressionBodyProperty => this.RecursiveExpressionBodyProperty;

        public int RecursiveStatementBodyProperty
        {
            get
            {
                return this.RecursiveStatementBodyProperty;
            }
        }

        public int RecursiveExpressionBodyMethod() => this.RecursiveExpressionBodyMethod();

        public int RecursiveExpressionBodyMethod(int value) => this.RecursiveExpressionBodyMethod(value);

        public int RecursiveStatementBodyMethod()
        {
            return this.RecursiveStatementBodyMethod();
        }

        public int RecursiveStatementBodyMethod(int value)
        {
            return this.RecursiveStatementBodyMethod(value);
        }

        public void Meh()
        {
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(1);
            // value = value;
        }

        private static int RecursiveStatementBodyMethodWithOptionalParameter(int value, IEnumerable<int> values = null)
        {
            if (values == null)
            {
                return RecursiveStatementBodyMethodWithOptionalParameter(value, new[] { value });
            }

            return value;
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