namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP008DontMixInjectedAndCreatedForMember>
    {
        [TestCase("this.stream.Dispose();")]
        [TestCase("this.stream?.Dispose();")]
        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public async Task DisposingCreatedField(string disposeCall)
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        
        public void Dispose()
        {
            this.stream.Dispose();
        }
    }";
            testCode = testCode.AssertReplace("this.stream.Dispose();", disposeCall);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingCreatedFieldInVirtualDispose()
        {
            var testCode = @"
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
            }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task HandlesRecursion()
        {
            var testCode = @"
    using System;

    public class Foo
    {
        private readonly IDisposable foo = Forever();

        private static IDisposable Forever()
        {
            return Forever();
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        public async Task PropertyWithCreatedValue(string property)
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream { get; } = File.OpenRead(string.Empty);
}";
            testCode = testCode.AssertReplace("public Stream Stream { get; }", property);
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyWithBackingFieldCreatedValue()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        [TestCase("public Stream Stream { get; protected set; }")]
        [TestCase("public Stream Stream { get; set; }")]
        public async Task PropertyWithInjectedValue(string property)
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    public Foo(Stream stream)
    {
        this.Stream = stream;
    }

    public Stream Stream { get; }
}";
            testCode = testCode.AssertReplace("public Stream Stream { get; }", property);
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedListOfInt()
        {
            var testCode = @"
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        private readonly List<int> ints;

        public Foo(List<int> ints)
        {
            this.ints = ints;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedListOfT()
        {
            var testCode = @"
    using System;
    using System.Collections.Generic;

    public class Foo<T>
    {
        private readonly List<T> values;

        public Foo(List<T> values)
        {
            this.values = values;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInClassThatIsNotIDisposable()
        {
            var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInClassThatIsIDisposable()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectingIntoPrivateCtor()
        {
            var disposableCode = @"
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }";

            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(disposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [TestCase("private set")]
        [TestCase("protected set")]
        [TestCase("set")]
        public async Task PropertyWithBackingFieldInjectedValue(string setter)
        {
            var testCode = @"
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
}";
            testCode = testCode.AssertReplace("private set", setter);
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task GenericTypeWithPropertyAndIndexer()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task LocalSwapCachedDisposableDictionary()
        {
            var disposableDictionaryCode = @"
using System;
using System.Collections.Generic;

public class DisposableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDisposable
{
    public void Dispose()
    {
    }
}";

            var testCode = @"
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
}";

            await this.VerifyHappyPathAsync(disposableDictionaryCode, testCode).ConfigureAwait(false);
        }
    }
}