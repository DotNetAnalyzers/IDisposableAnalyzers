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
            Assert.Pass($"Count: {AllAnalyzers.Length}");
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void PropertyChangedAnalyzersSln(DiagnosticAnalyzer analyzer)
        {
            AnalyzerAssert.Valid(analyzer, Solution);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public void PropertyChangedAnalyzersProject(DiagnosticAnalyzer analyzer)
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

            var fooCode = @"
namespace RoslynSandbox
{
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
        public async Task<string> Bar1Async()
        {
            using (var stream = await ReadAsync(string.Empty))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadLine();
                }
            }
        }

        public async Task<string> Bar2Async()
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
            var sources = new[]
                          {
                              disposableCode,
                              fooListCode,
                              fooCode,
                              fooBaseCode,
                              fooImplCode,
                              withOptionalParameterCode,
                              reactiveCode,
                              lazyCode,
                              asyncCode,
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
            var converterCode = @"
namespace RoslynSandbox
{
     using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    public class ValidationErrorToStringConverter : IValueConverter
    {
        /// <summary> Gets the default instance </summary>
        public static readonly ValidationErrorToStringConverter Default = new ValidationErrorToStringConverter();

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text;
            }

            if (value is ValidationResult result)
            {
                return this.Convert(result.ErrorContent, targetType, parameter, culture);
            }

            if (value is ValidationError error)
            {
                return this.Convert(error.ErrorContent, targetType, parameter, culture);
            }

            return value;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($""{this.GetType().Name} only supports one-way conversion."");
        }
    }
}";
            AnalyzerAssert.Valid(analyzer, testCode, converterCode);
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
