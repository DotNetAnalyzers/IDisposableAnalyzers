namespace IDisposableAnalyzers.Test.IDISP008DontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(FieldAndPropertyDeclarationAnalyzer))]
    [TestFixture(typeof(AssignmentAnalyzer))]
    internal partial class ValidCode<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly T Analyzer = new T();

        [TestCase("private Stream Stream")]
        [TestCase("protected Stream Stream")]
        public void MutableFieldInSealed(string property)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private Stream Stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}".AssertReplace("private Stream Stream", property);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("public Stream Stream { get; protected set; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        [TestCase("protected Stream Stream { get; set; }")]
        public void MutablePropertyInSealed(string property)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        public Stream Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}".AssertReplace("public Stream Stream { get; set; }", property);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("this.stream.Dispose();")]
        [TestCase("this.stream?.Dispose();")]
        [TestCase("stream.Dispose();")]
        [TestCase("stream?.Dispose();")]
        public void DisposingCreatedField(string disposeCall)
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingCreatedFieldInVirtualDispose()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void HandlesRecursion()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        public void PropertyWithCreatedValue(string property)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        public C()
        {
            this.Stream.Dispose();
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
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
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        private Stream stream = File.OpenRead(string.Empty);

        public C()
        {
            this.stream.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }

        public void Dispose()
        {
            this.stream?.Dispose();
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

    public sealed class C
    {
        public C(Stream stream)
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

    public class C
    {
        private readonly List<int> ints;

        public C(List<int> ints)
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

    public class C<T>
    {
        private readonly List<T> values;

        public C(List<T> values)
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

    public sealed class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
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

    public sealed class C
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void GenericTypeWithPropertyAndIndexer()
        {
            var testCode = @"
namespace RoslynSandbox
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

    public class C
    {
        private static readonly DisposableDictionary<int, Stream> Cache = new DisposableDictionary<int, Stream>();

        private Stream current;

        public void SetCurrent(int number)
        {
            this.current = Cache[number];
            this.current = Cache[number + 1];
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, disposableDictionaryCode, testCode);
        }

        [Test]
        public void PublicMethodRefIntParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        public bool TryGetStream(ref int number)
        {
            number = 1;
            return true;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PublicMethodRefStringParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        public bool TryGetStream(ref string text)
        {
            text = new string('a', 1);
            return true;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
