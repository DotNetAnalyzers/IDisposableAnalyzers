namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP006ImplementIDisposable>
    {
        public class WhenDisposing : NestedHappyPathVerifier<HappyPath>
        {
            [TestCase("this.stream.Dispose();")]
            [TestCase("this.stream?.Dispose();")]
            [TestCase("stream.Dispose();")]
            [TestCase("stream?.Dispose();")]
            public async Task DisposingField(string disposeCall)
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
            public async Task DisposingFieldInVirtualDispose()
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
            public async Task DisposingFieldInVirtualDispose2()
            {
                var testCode = @"
using System;

public class Foo : IDisposable
{
    private readonly IDisposable _disposable = new Disposable();
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposable.Dispose();
        }
    }

    protected void VerifyDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DisposingFieldInExpressionBodyDispose()
            {
                var testCode = @"
using System;
class Goof : IDisposable {
    IDisposable _disposable;
    public void Create()  => _disposable = new Disposable();
    public void Dispose() => _disposable.Dispose();
}";
                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DisposingFieldAsCast()
            {
                var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            var disposable = this.stream as IDisposable;
            disposable?.Dispose();
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DisposingFieldInlineAsCast()
            {
                var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.stream as IDisposable)?.Dispose();
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DisposingFieldExplicitCast()
            {
                var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            var disposable = (IDisposable)this.stream;
            disposable.Dispose();
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DisposingFieldInlineExplicitCast()
            {
                var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            ((IDisposable)this.stream).Dispose();
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DisposingPropertyWhenInitializedInProperty()
            {
                var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; set; }
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }";

                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DisposingPropertyWhenInitializedInline()
            {
                var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Stream Stream { get; set; } = File.OpenRead(string.Empty);
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }";

                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnorePassedInViaCtor1()
            {
                var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable bar;
        
        public Foo(IDisposable bar)
        {
            this.bar = bar;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnorePassedInViaCtor2()
            {
                var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable _bar;
        
        public Foo(IDisposable bar)
        {
            _bar = bar;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnorePassedInViaCtor3()
            {
                var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable _bar;
        
        public Foo(IDisposable bar)
        {
            _bar = bar;
        }

        public void Dispose()
        {
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [TestCase("disposables.First();")]
            [TestCase("disposables.Single();")]
            public async Task IgnoreLinq(string linq)
            {
                var testCode = @"
using System;
using System.Linq;

public sealed class Foo
{
    private readonly IDisposable _bar;
        
    public Foo(IDisposable[] disposables)
    {
        _bar = disposables.First();
    }
}";
                testCode = testCode.AssertReplace("disposables.First();", linq);
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoredWhenNotAssigned()
            {
                var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly IDisposable bar;
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoredWhenBackingField()
            {
                var testCode = @"
    using System.IO;

    public sealed class Foo
    {
        private Stream stream;

        public Stream Stream
        {
            get { return this.stream; }
            set { this.stream = value; }
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreFieldThatIsNotDisposable()
            {
                var testCode = @"
    public class Foo
    {
        private readonly object bar = new object();
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreFieldThatIsNotDisposableAssignedWithMethod1()
            {
                var testCode = @"
    public class Foo
    {
        private readonly object bar = Meh();

        private static object Meh() => new object();
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreFieldThatIsNotDisposableAssignedWIthMethod2()
            {
                var testCode = @"
    public class Foo
    {
        private readonly object bar = string.Copy(string.Empty);
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoredStaticField()
            {
                var testCode = @"
    using System.IO;

    public sealed class Foo
    {
        private static Stream stream = File.OpenRead(string.Empty);
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreTask()
            {
                var testCode = @"
    using System.Threading.Tasks;

    public sealed class Foo
    {
        private readonly Task stream = Task.Delay(0);
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreTaskOfInt()
            {
                var testCode = @"
    using System.Threading.Tasks;

    public sealed class Foo
    {
        private readonly Task<int> stream = Task.FromResult(0);
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task FieldOfTypeArrayOfInt()
            {
                var testCode = @"
    public sealed class Foo
    {
        private readonly int[] ints = new[] { 1, 2, 3 };
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task PropertyWithBackingFieldOfTypeArrayOfInt()
            {
                var testCode = @"
    public sealed class Foo
    {
        private int[] ints;

        public int[] Ints
        {
            get
            {
                return this.ints ?? (this.ints = new int[] { });
            }

            set
            {
                this.ints = value;
            }
        }

        public bool HasInts => (this.ints != null) && (this.ints.Length > 0);
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}