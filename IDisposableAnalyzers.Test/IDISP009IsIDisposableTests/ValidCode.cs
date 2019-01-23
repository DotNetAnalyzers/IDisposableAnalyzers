namespace IDisposableAnalyzers.Test.IDISP009IsIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DisposeMethodAnalyzer();

        [Test]
        public void DisposingCreatedFieldInVirtualDispose()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.stream.Dispose();
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void HandlesRecursion()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly IDisposable foo = Forever();

        private static IDisposable Forever()
        {
            return Forever();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode)
                      ;
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        public void PropertyWithCreatedValue(string property)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}".AssertReplace("public Stream Stream { get; }", property);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PropertyWithBackingFieldCreatedValue()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        private Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            this.stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        [TestCase("public Stream Stream { get; protected set; }")]
        [TestCase("public Stream Stream { get; set; }")]
        public void PropertyWithInjectedValue(string property)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public Foo(Stream stream)
        {
            this.Stream = stream;
        }

        public Stream Stream { get; }
    }
}".AssertReplace("public Stream Stream { get; }", property);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedListOfInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        private readonly List<int> ints;

        public Foo(List<int> ints)
        {
            this.ints = ints;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedListOfT()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo<T>
    {
        private readonly List<T> values;

        public Foo(List<T> values)
        {
            this.values = values;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedInClassThatIsNotIDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectedInClassThatIsIDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InjectingIntoPrivateCtor()
        {
            var disposableCode = @"
namespace RoslynSandbox
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo()
            : this(new Disposable())
        {
        }

        private Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, disposableCode, testCode);
        }

        [TestCase("private set")]
        [TestCase("protected set")]
        [TestCase("set")]
        public void PropertyWithBackingFieldInjectedValue(string setter)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        private static readonly Stream StaticStream = File.OpenRead(string.Empty);
        private Stream stream;

        public Foo(Stream stream)
        {
            this.stream = stream;
            this.stream = StaticStream;
            this.Stream = stream;
            this.Stream = StaticStream;
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}".AssertReplace("private set", setter);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void GenericTypeWithPropertyAndIndexer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public sealed class Foo<T>
    {
        private T value;
        private List<T> values = new List<T>();

        public T Value
        {
            get { return this.value; }
            private set { this.value = value; }
        }

        /// <inheritdoc/>
        public T this[int index]
        {
            get
            {
                return this.values[index];
            }

            set
            {
                this.values[index] = value;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalSwapCachedDisposableDictionary()
        {
            var disposableDictionaryCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class DisposableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.IO;

    public class Foo
    {
        private readonly DisposableDictionary<int, Stream> Cache = new DisposableDictionary<int, Stream>();

        private Stream current;

        public void SetCurrent(int number)
        {
            this.current = this.Cache[number];
            this.current = this.Cache[number + 1];
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, disposableDictionaryCode, testCode);
        }

        [Test]
        public void IgnoreTestMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class Foo
    {
        [Test]
        public void Dispose()
        {
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenImplementingInterface()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public abstract class Foo : IFoo
    {
        private bool disposed;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
            }
        }

        protected virtual void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }

    public interface IFoo : IDisposable
    {
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenImplementingIHttpModule()
        {
            var testCode = @"
namespace System.Web
{
  public interface IHttpModule
  {
    void Init(HttpApplication context);
    void Dispose();
  }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenSubclassingAndImplementingTwoInterfaces()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Windows;

    public sealed class Foo : DependencyObject, IFoo, IDisposable
    {
        public void Dispose()
        {
        }
    }

    public interface IFoo
    {
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
