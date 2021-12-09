namespace IDisposableAnalyzers.Test.IDISP009IsIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DisposeMethodAnalyzer Analyzer = new();

        [Test]
        public static void DisposingCreatedFieldInVirtualDispose()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void HandlesRecursion()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable foo = Forever();

        private static IDisposable Forever()
        {
            return Forever();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code)
                      ;
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        public static void PropertyWithCreatedValue(string property)
        {
            var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}".AssertReplace("public Stream Stream { get; }", property);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void PropertyWithBackingFieldCreatedValue()
        {
            var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private Stream stream = File.OpenRead(string.Empty);

        public C()
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        [TestCase("public Stream Stream { get; protected set; }")]
        [TestCase("public Stream Stream { get; set; }")]
        public static void PropertyWithInjectedValue(string property)
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public C(Stream stream)
        {
            this.Stream = stream;
        }

        public Stream Stream { get; }
    }
}".AssertReplace("public Stream Stream { get; }", property);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedListOfInt()
        {
            var code = @"
namespace N
{
    using System.Collections.Generic;

    public class C
    {
        private readonly List<int> ints;

        public C(List<int> ints)
        {
            this.ints = ints;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedListOfT()
        {
            var code = @"
namespace N
{
    using System.Collections.Generic;

    public class C<T>
    {
        private readonly List<T> values;

        public C(List<T> values)
        {
            this.values = values;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedInClassThatIsNotIDisposable()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectedInClassThatIsIDisposable()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InjectingIntoPrivateCtor()
        {
            var disposableCode = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, disposableCode, code);
        }

        [TestCase("private set")]
        [TestCase("protected set")]
        [TestCase("set")]
        public static void PropertyWithBackingFieldInjectedValue(string setter)
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        private static readonly Stream StaticStream = File.OpenRead(string.Empty);
        private Stream stream;

        public C(Stream stream)
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void GenericTypeWithPropertyAndIndexer()
        {
            var code = @"
#nullable disable
namespace N
{
    using System.Collections.Generic;

    public sealed class C<T>
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void LocalSwapCachedDisposableDictionary()
        {
            var disposableDictionaryOfTKeyTvalue = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public class DisposableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDisposable
        where TKey : notnull
    {
        public void Dispose()
        {
        }
    }
}";

            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        private readonly DisposableDictionary<int, Stream> Cache = new DisposableDictionary<int, Stream>();

        private Stream? current;

        public void SetCurrent(int number)
        {
            this.current = this.Cache[number];
            this.current = this.Cache[number + 1];
        }
    }
}";

            RoslynAssert.Valid(Analyzer, disposableDictionaryOfTKeyTvalue, code);
        }

        [Test]
        public static void IgnoreTestMethod()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [Test]
        public void Dispose()
        {
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenImplementingInterface()
        {
            var code = @"
namespace N
{
    using System;

    public abstract class C : IC
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

    public interface IC : IDisposable
    {
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSubclassingAndImplementingTwoInterfaces()
        {
            var code = @"
namespace N
{
    using System;
    using System.Windows;

    public sealed class C : DependencyObject, IC, IDisposable
    {
        public void Dispose()
        {
        }
    }

    public interface IC
    {
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
