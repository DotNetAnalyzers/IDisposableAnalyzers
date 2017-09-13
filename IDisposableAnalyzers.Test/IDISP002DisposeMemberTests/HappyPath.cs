namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP002DisposeMember>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        [TestCase("this.stream.Dispose();")]
        [TestCase("this.stream?.Dispose();")]
        [TestCase("Stream.Dispose();")]
        [TestCase("Stream?.Dispose();")]
        [TestCase("this.Stream.Dispose();")]
        [TestCase("this.Stream?.Dispose();")]
        [TestCase("Calculated.Dispose();")]
        [TestCase("Calculated?.Dispose();")]
        [TestCase("this.Calculated.Dispose();")]
        [TestCase("this.Calculated?.Dispose();")]
        public async Task DisposingField(string disposeCall)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private Stream stream = File.OpenRead(string.Empty);

        public Stream Calculated => this.stream;

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }
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
            await this.VerifyHappyPathAsync(disposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingFieldInExpressionBodyDispose()
        {
            var disposableCode = @"
using System;
class Disposable : IDisposable {
    public void Dispose() { }
}";

            var testCode = @"
using System;
class Goof : IDisposable {
    IDisposable _disposable;
    public void Create()  => _disposable = new Disposable();
    public void Dispose() => _disposable.Dispose();
}";
            await this.VerifyHappyPathAsync(disposableCode, testCode)
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
        public async Task DisposingPropertyInBaseClass()
        {
            var baseClassCode = @"
    using System;
    using System.IO;

    public abstract class FooBase : IDisposable
    {
        public abstract Stream Stream { get; }
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }";

            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : FooBase
    {
        public override Stream Stream { get; } = File.OpenRead(string.Empty);
    }";

            await this.VerifyHappyPathAsync(baseClassCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingPropertyInVirtualDisposeInBaseClass()
        {
            var baseClassCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public abstract class FooBase : IDisposable
    {
        private bool disposed;

        public abstract Stream Stream { get; }

        /// <inheritdoc/>
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
                this.Stream?.Dispose();
            }
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo : FooBase
    {
        public override Stream Stream { get; } = File.OpenRead(string.Empty);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }
    }
}";

            await this.VerifyHappyPathAsync(baseClassCode, testCode)
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
        public async Task IgnoredWhenBackingFieldWithMethodSettingPropertyToNull()
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

    public void Meh()
    {
        var temp = this.Stream;
        this.Stream = null;
        this.stream = temp;
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
        public async Task DisposingPropertyInBase()
        {
            var fooCode = @"
using System;
using System.IO;

public class Foo : IDisposable
{
    public virtual Stream Stream { get; } = File.OpenRead(string.Empty);
    private bool disposed;

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Stream.Dispose();
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

            var barCode = @"
using System.IO;

public class Bar : Foo
{
    public override Stream Stream { get; }
}";
            await this.VerifyHappyPathAsync(fooCode, barCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenCallingBaseDispose()
        {
            var fooBaseCode = @"
using System;

public abstract class FooBase : IDisposable
{
    private readonly IDisposable disposable = new Disposable();
    private bool disposed;

    /// <inheritdoc/>
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
            this.disposable.Dispose();
        }
    }
}";
            var testCode = @"
public class Foo : FooBase
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}";

            await this.VerifyHappyPathAsync(DisposableCode, fooBaseCode, testCode)
                      .ConfigureAwait(false);
        }
    }
}