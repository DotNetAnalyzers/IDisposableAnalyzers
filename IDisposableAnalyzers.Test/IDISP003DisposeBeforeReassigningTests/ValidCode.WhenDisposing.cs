namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public static partial class Valid<T>
    {
        [Test]
        public static void DisposingVariable()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public static void DisposeBeforeAssigningInIfElse(string dispose)
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void Meh(bool b)
        {
            Stream stream = File.OpenRead(string.Empty);
            if (b)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
            }

            stream.Dispose();
        }
    }
}".AssertReplace("stream.Dispose();", dispose);
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public static void DisposeBeforeAssigningBeforeIfElse(string dispose)
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void Meh(bool b)
        {
            Stream stream = File.OpenRead(string.Empty);
            stream.Dispose();
            if (b)
            {
                stream = null;
            }
            else
            {
                stream = File.OpenRead(string.Empty);
            }

            stream?.Dispose();
        }
    }
}".AssertReplace("stream.Dispose();", dispose);
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public static void DisposeFieldBeforeIfElseReassigning(string dispose)
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public class C
    {
        private Stream stream = File.OpenRead(string.Empty);

        public void Meh()
        {
            this.stream.Dispose();
            if (true)
            {
                this.stream = null;
            }
            else
            {
                this.stream = File.OpenRead(string.Empty);
            }
        }
    }
}".AssertReplace("stream.Dispose();", dispose);
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void DisposingParameter()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public void M(Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void DisposingFieldInCtor()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        public C()
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void DisposingFieldInMethod()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public void Meh()
        {
            this.stream.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConditionallyDisposingFieldInMethod()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private Stream stream;

        public void Meh()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConditionallyDisposingUnderscoreFieldInMethod()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private Stream _stream;

        public void Meh()
        {
            _stream?.Dispose();
            _stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void DisposingUnderscoreFieldInMethod()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private Stream _stream;

        public void Meh()
        {
            _stream.Dispose();
            _stream = File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssigningFieldInLambda()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private Disposable disposable;

        public C(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable?.Dispose();
                this.disposable = new Disposable();
            });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void AssigningBackingFieldInLambda()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private IDisposable disposable;

        public C(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable?.Dispose();
                this.Disposable = new Disposable();
            });
        }

        public IDisposable Disposable
        {
            get { return this.disposable; }
            private set { this.disposable = value; }
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void AssigningSerialDisposableBackingFieldInLambda()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private readonly SerialDisposable disposable = new SerialDisposable();

        public C(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.Disposable = new Disposable();
            });
        }

        public IDisposable Disposable
        {
            get { return this.disposable.Disposable; }
            private set { this.disposable.Disposable = value; }
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void AssigningSerialDisposableFieldInLambda()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly IDisposable subscription;
        private readonly SerialDisposable disposable = new SerialDisposable();

        public C(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ =>
            {
                this.disposable.Disposable = new Disposable();
            });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
            this.subscription?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void DisposingPreviousAssignedToLocal()
        {
            var testCode = @"
namespace N
{
    using System;

    class C : IDisposable
    {
        private Disposable current;

        public void Test()
        {
            var old = current;
            current = new Disposable();
            if (old != current) old.Dispose();
        }

        public void Dispose()
        {
            current?.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public static void WhileLoop(string call)
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public C(int i)
        {
            Stream stream = File.OpenRead(string.Empty);
            while (i > 0)
            {
                stream.Dispose();
                stream = File.OpenRead(string.Empty);
                i--;
            }

            stream.Dispose();
        }
    }
}".AssertReplace("stream.Dispose();", call);

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
