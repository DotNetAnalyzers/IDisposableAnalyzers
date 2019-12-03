namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new CreationAnalyzer();

        private const string DisposableCode = @"
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

        [TestCase("new Disposable()")]
        [TestCase("File.OpenRead(fileName)")]
        [TestCase("(File.OpenRead(fileName), 1)")]
        [TestCase("(File.OpenRead(fileName), File.OpenRead(fileName))")]
        [TestCase("Tuple.Create(File.OpenRead(fileName), 1)")]
        [TestCase("Tuple.Create(File.OpenRead(fileName), File.OpenRead(fileName))")]
        [TestCase("new Tuple<FileStream, int>(File.OpenRead(fileName), 1)")]
        [TestCase("new List<FileStream> { File.OpenRead(fileName) }")]
        [TestCase("new List<FileStream> { File.OpenRead(fileName), File.OpenRead(fileName) }")]
        [TestCase("new List<Disposable> { new Disposable() }")]
        [TestCase("new List<Disposable> { new Disposable(), new Disposable() }")]
        public static void AssigningLocal(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public sealed class C
    {
        public C(string fileName)
        {
            var disposable = new Disposable();
        }
    }
}".AssertReplace("new Disposable()", expression);
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [TestCase("new Disposable()")]
        [TestCase("File.OpenRead(fileName)")]
        [TestCase("(File.OpenRead(fileName), 1)")]
        [TestCase("(File.OpenRead(fileName), File.OpenRead(fileName))")]
        [TestCase("Tuple.Create(File.OpenRead(fileName), 1)")]
        [TestCase("Tuple.Create(File.OpenRead(fileName), File.OpenRead(fileName))")]
        [TestCase("new Tuple<FileStream, int>(File.OpenRead(fileName), 1)")]
        [TestCase("new List<FileStream> { File.OpenRead(fileName) }")]
        [TestCase("new List<FileStream> { File.OpenRead(fileName), File.OpenRead(fileName) }")]
        [TestCase("new List<Disposable> { new Disposable() }")]
        [TestCase("new List<Disposable> { new Disposable(), new Disposable() }")]
        public static void AssigningField(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public sealed class C
    {
        private readonly object disposable;

        public C(string fileName)
        {
            this.disposable = new Disposable();
        }
    }
}".AssertReplace("new Disposable()", expression);
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [Test]
        public static void RealisticExtensionMethodClass()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IfTry()
        {
            var code = @"
namespace N
{
    public class C
    {
        private void M()
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReadAsyncCall()
        {
            var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;

    public static class C
    {
        public static async Task<string> MAsync()
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReadAsyncConfigureAwait()
        {
            var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;

    public static class C
    {
        public static async Task<string> MAsync()
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenGettingPropertyOfDisposable()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
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

    class M
    {
        public M(C c)
        {
            var fooValue = c.Value;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenGettingPropertyOfProperty()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
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

    class M
    {
        public M(C c)
        {
            this.C = c;
            var fooValue = this.C.Value;
        }

        public C C { get; }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AddingFileOpenReadToList()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public sealed class C
    {
        private List<Stream> streams = new List<Stream>();
        public C()
        {
            streams.Add(File.OpenRead(string.Empty));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AddingNewDisposableToList()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public sealed class C
    {
        private List<IDisposable> disposables = new List<IDisposable>();

        public C()
        {
            this.disposables.Add(new Disposable());
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [Test]
        public static void AddingNewDisposableToListThatIsDisposed()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    internal sealed class ListOfObject : IDisposable
    {
        private readonly List<object> disposables = new List<object>();

        public ListOfObject()
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
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [Test]
        public static void AddingNewDisposableToListOfObjectThatIsTouchedInDisposeMethod()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public sealed class C : IDisposable
    {
        private List<object> disposables = new List<object>();

        public C()
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
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [TestCase("File.Create(fileName).Dispose()")]
        [TestCase("File.Create(fileName)?.Dispose()")]
        public static void DisposingInSameStatement(string statement)
        {
            var code = @"
namespace N
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
}".AssertReplace("File.Create(fileName).Dispose()", statement);
            RoslynAssert.Valid(Analyzer, DisposableCode, code);
        }

        [TestCase("Stream().Dispose()")]
        [TestCase("Stream()?.Dispose()")]
        [TestCase("this.Stream().Dispose()")]
        [TestCase("this.Stream()?.Dispose()")]
        public static void DisposingMethodReturnValue(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public Stream Stream() => File.OpenRead(string.Empty);

        public void M()
        {
            this.Stream().Dispose();
        }
    }
}".AssertReplace("this.Stream().Dispose()", expression);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("Stream().Dispose()")]
        [TestCase("Stream()?.Dispose()")]
        public static void DisposingStaticMethodReturnValue(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static Stream Stream() => File.OpenRead(string.Empty);

        public static void M()
        {
            Stream().Dispose();
        }
    }
}".AssertReplace("Stream().Dispose()", expression);
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("Stream.Dispose()")]
        [TestCase("Stream?.Dispose()")]
        [TestCase("this.Stream.Dispose()")]
        [TestCase("this.Stream?.Dispose()")]
        public static void DisposingPropertyReturnValue(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public Stream Stream => File.OpenRead(string.Empty);

        public void M()
        {
            this.Stream.Dispose();
        }
    }
}".AssertReplace("this.Stream.Dispose()", expression);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("Stream.Dispose()")]
        [TestCase("Stream?.Dispose()")]
        public static void DisposingStaticPropertyReturnValue(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static Stream Stream => File.OpenRead(string.Empty);

        public static void M()
        {
            Stream.Dispose();
        }
    }
}".AssertReplace("Stream.Dispose()", expression);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AddFileOpenReadToListOfObjectField()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public sealed class C : IDisposable
    {
        private readonly List<object> streams = new List<object>();

        public C()
        {
            this.streams.Add(File.OpenRead(string.Empty));
        }

        public void Dispose()
        {
            foreach (var item in this.streams)
            {
                (item as IDisposable)?.Dispose();
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("using (await Task.Run(() => File.OpenRead(fileName)))")]
        [TestCase("using (await Task.Run(() => File.OpenRead(fileName)).ConfigureAwait(false))")]
        [TestCase("using (var stream = await Task.Run(() => File.OpenRead(fileName)))")]
        [TestCase("using (var stream = await Task.Run(() => File.OpenRead(fileName)).ConfigureAwait(false))")]
        [TestCase("using (await Task.FromResult(File.OpenRead(fileName)))")]
        [TestCase("using (await Task.FromResult(File.OpenRead(fileName)).ConfigureAwait(false))")]
        [TestCase("using (var stream = await Task.FromResult(File.OpenRead(fileName)))")]
        [TestCase("using (var stream = await Task.FromResult(File.OpenRead(fileName)).ConfigureAwait(false))")]
        public static void UsingAwaited(string statement)
        {
            var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;

    class C
    {
        static async Task M(string fileName)
        {
            using (await Task.FromResult(File.OpenRead(fileName)))
            {
            }
        }
    }
}".AssertReplace("using (await Task.FromResult(File.OpenRead(fileName)))", statement);
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("await Task.Run(() => File.OpenRead(fileName))")]
        [TestCase("await Task.Run(() => File.OpenRead(fileName)).ConfigureAwait(false)")]
        [TestCase("await Task.FromResult(File.OpenRead(fileName))")]
        [TestCase("await Task.FromResult(File.OpenRead(fileName)).ConfigureAwait(false)")]
        public static void AssigningAwaitedToLocal(string statement)
        {
            var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;

    class C
    {
        static async Task M(string fileName)
        {
            var stream = await Task.FromResult(File.OpenRead(fileName));
            stream.Dispose();
        }
    }
}".AssertReplace("await Task.FromResult(File.OpenRead(fileName))", statement);
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("await Task.Run(() => File.OpenRead(fileName))")]
        [TestCase("await Task.Run(() => File.OpenRead(fileName)).ConfigureAwait(false)")]
        [TestCase("await Task.FromResult(File.OpenRead(fileName))")]
        [TestCase("await Task.FromResult(File.OpenRead(fileName)).ConfigureAwait(false)")]
        public static void AssigningAwaitedToField(string statement)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    sealed class C : IDisposable
    {
        private Stream stream;

        async Task M(string fileName)
        {
            stream?.Dispose();
            this.stream = await Task.FromResult(File.OpenRead(fileName));
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}".AssertReplace("await Task.FromResult(File.OpenRead(fileName))", statement);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DispatcherInvoke()
        {
            var code = @"
namespace N
{
    using System.Threading.Tasks;

    public sealed class C
    {
        public async Task M()
        {
            await System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => Task.FromResult(42)).ConfigureAwait(false);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
