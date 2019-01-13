#pragma warning disable SA1203 // Constants must appear before fields
namespace IDisposableAnalyzers.Test.IDISP004DontIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(IDISP004DontIgnoreCreated))]
    [TestFixture(typeof(ObjectCreationAnalyzer))]
    internal partial class ValidCode<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly DiagnosticAnalyzer Analyzer = new T();

        private const string DisposableCode = @"
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

        [Test]
        public void AssigningLocal()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        public Foo()
        {
            var disposable = new Disposable();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void RealisticExtensionMethodClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal static class EnumerableExt
    {
        internal static bool TryElementAt<TCollection, TItem>(this TCollection source, int index, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            result = default(TItem);
            if (source == null)
            {
                return false;
            }

            if (source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        internal static bool TrySingle<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(TItem);
            return false;
        }

        internal static bool TrySingle<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryFirst<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 0)
            {
                result = default(TItem);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryFirst<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryLast<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 0)
            {
                result = default(TItem);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        internal static bool TryLast<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
             where TCollection : IReadOnlyList<TItem>
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IfTry()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private void Bar()
        {
            int value;
            if(Try(out value))
            {
            }
        }

        private bool Try(out int value)
        {
            value = 1;
            return true;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReadAsyncCall()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public static class Foo
    {
        public static async Task<string> BarAsync()
        {
            using (var stream = await ReadAsync(string.Empty))
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
}
";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReadAsyncConfigureAwait()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;
    using System.Threading.Tasks;

    public static class Foo
    {
        public static async Task<string> BarAsync()
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
}
";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenGettingPropertyOfDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private bool disposed;
        private int value;

        public int Value
        {
            get
            {
                this.ThrowIfDisposed();
                return this.value;
            }

            set
            {
                this.ThrowIfDisposed();
                this.value = value;
            }
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }

    class Bar
    {
        public Bar(Foo foo)
        {
            var fooValue = foo.Value;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenGettingPropertyOfProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private bool disposed;
        private int value;

        public int Value
        {
            get
            {
                this.ThrowIfDisposed();
                return this.value;
            }

            set
            {
                this.ThrowIfDisposed();
                this.value = value;
            }
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }

    class Bar
    {
        public Bar(Foo foo)
        {
            this.Foo = foo;
            var fooValue = this.Foo.Value;
        }

        public Foo Foo { get; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AddingFileOpenReadToList()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public sealed class Foo
    {
        private List<Stream> streams = new List<Stream>();
        public Foo()
        {
            streams.Add(File.OpenRead(string.Empty));
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AddingNewDisposableToList()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public sealed class Foo
    {
        private List<IDisposable> disposables = new List<IDisposable>();

        public Foo()
        {
            this.disposables.Add(new Disposable());
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AddingNewDisposableToListOfObjectThatIsTouchedInDisposeMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public sealed class Foo : IDisposable
    {
        private List<object> disposables = new List<object>();

        public Foo()
        {
            this.disposables.Add(new Disposable());
        }

        public void Dispose()
        {
            foreach (var disposable in this.disposables)
            {
                (disposable as IDisposable)?.Dispose();
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void DisposingInSameStatement()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public static class C
    {
        public static void Touch(string fileName)
        {
            File.Create(fileName).Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }
    }
}
