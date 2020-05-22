namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class WhenDisposing
        {
            [TestCase("this.stream.Dispose();")]
            [TestCase("this.stream?.Dispose();")]
            [TestCase("stream.Dispose();")]
            [TestCase("stream?.Dispose();")]
            public static void DisposingField(string disposeCall)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        
        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}".AssertReplace("this.stream.Dispose();", disposeCall);
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void DisposingFieldInVirtualDispose()
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
            public static void DisposingFieldInVirtualDispose2()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
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
    }
}";
                RoslynAssert.Valid(Analyzer, Disposable, code);
            }

            [Test]
            public static void DisposingFieldInExpressionBodyDispose()
            {
                var code = @"
namespace N
{
    using System;

    class Goof : IDisposable {
        IDisposable _disposable;
        public void Create()  => _disposable = new Disposable();
        public void Dispose() => _disposable.Dispose();
    }
}";
                RoslynAssert.Valid(Analyzer, Disposable, code);
            }

            [Test]
            public static void DisposingFieldAsCast()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            var disposable = this.stream as IDisposable;
            disposable?.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void DisposingFieldInlineAsCast()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            (this.stream as IDisposable)?.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void DisposingFieldExplicitCast()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            var disposable = (IDisposable)this.stream;
            disposable.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void DisposingFieldInlineExplicitCast()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly object stream =  File.OpenRead(string.Empty);

        public void Dispose()
        {
            ((IDisposable)this.stream).Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void DisposingPropertyWhenInitializedInProperty()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void DisposingPropertyWhenInitializedInline()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        public Stream Stream { get; private set; } = File.OpenRead(string.Empty);
        
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnorePassedInViaCtor1()
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
            public static void IgnorePassedInViaCtor2()
            {
                var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        private readonly IDisposable _disposable;
        
        public C(IDisposable disposable)
        {
            _disposable = disposable;
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnorePassedInViaCtor3()
            {
                var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable _disposable;
        
        public C(IDisposable disposable)
        {
            _disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("disposables.First();")]
            [TestCase("disposables.Single();")]
            public static void IgnoreLinq(string linq)
            {
                var code = @"
namespace N
{
    using System;
    using System.Linq;

    public sealed class C
    {
        private readonly IDisposable _disposable;
        
        public C(IDisposable[] disposables)
        {
            _disposable = disposables.First();
        }
    }
}".AssertReplace("disposables.First();", linq);
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoredWhenNotAssigned()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly IDisposable disposable;
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoredWhenBackingField()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private Stream stream;

        public Stream Stream
        {
            get { return this.stream; }
            set { this.stream = value; }
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoreFieldThatIsNotDisposable()
            {
                var code = @"
namespace N
{
    public class C
    {
        private readonly object f = new object();
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoreFieldThatIsNotDisposableAssignedWithMethod1()
            {
                var code = @"
namespace N
{
    public class C
    {
        private readonly object f = M();

        private static object M() => new object();
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoreFieldThatIsNotDisposableAssignedWIthMethod2()
            {
                var code = @"
namespace N
{
    public class C
    {
        private readonly object f = string.Copy(string.Empty);
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoredStaticField()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private static Stream stream = File.OpenRead(string.Empty);
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoreTask()
            {
                var code = @"
namespace N
{
    using System.Threading.Tasks;

    public sealed class C
    {
        private readonly Task stream = Task.Delay(0);
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoreTaskOfInt()
            {
                var code = @"
namespace N
{
    using System.Threading.Tasks;

    public sealed class C
    {
        private readonly Task<int> stream = Task.FromResult(0);
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void FieldOfTypeArrayOfInt()
            {
                var code = @"
namespace N
{
    public sealed class C
    {
        private readonly int[] ints = new[] { 1, 2, 3 };
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void PropertyWithBackingFieldOfTypeArrayOfInt()
            {
                var code = @"
namespace N
{
    public sealed class C
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
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void ExplicitImplementation()
            {
                var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly Disposable disposable = new Disposable();

        void IDisposable.Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, Disposable, code);
            }
        }
    }
}
