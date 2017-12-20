// ReSharper disable InconsistentNaming
#pragma warning disable SA1203 // Constants must appear before fields
namespace IDisposableAnalyzers.Test.IDISP005ReturnTypeShouldIndicateIDisposableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        private static readonly ReturnValueAnalyzer Analyzer = new ReturnValueAnalyzer();

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
        public void RealisticExtensionMethodClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal static class EnumerableExt
    {
        internal static bool TryGetAtIndex<TCollection, TItem>(this TCollection source, int index, out TItem result)
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

        internal static bool TryGetSingle<TCollection, TItem>(this TCollection source, out TItem result)
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

        internal static bool TryGetSingle<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
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

        internal static bool TryGetFirst<TCollection, TItem>(this TCollection source, out TItem result)
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

        internal static bool TryGetFirst<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
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

        internal static bool TryGetLast<TCollection, TItem>(this TCollection source, out TItem result)
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

        internal static bool TryGetLast<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
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
        public void VoidMethodReturn()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static void Meh()
        {
            return;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static object Meh()
        {
            return new object();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningDynamic()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static dynamic Meh()
        {
            return new object();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void GenericClassMethodReturningDynamicSubtract()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo<T>
    {
        private readonly T item1;
        private readonly T item2;

        private dynamic Meh() => (dynamic)item1 - (dynamic)item2; //Supersnyggt
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void GenericClassPropertyReturningDynamicSubtract()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo<T>
    {
        private readonly T item1;
        private readonly T item2;

        private dynamic Meh => (dynamic)item1 - (dynamic)item2; //Supersnyggt
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningThis()
        {
            var chunkCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public class Chunk<T>
    {
        private readonly List<T> items = new List<T>();
        private readonly object gate = new object();

        public Chunk<T> Add(T item)
        {
            lock (this.gate)
            {
                this.items.Add(item);
            }

            return this;
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public object Meh()
        {
            var chunk = new Chunk<int>();
            return chunk.Add(1);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, chunkCode, testCode);
        }

        [Test]
        public void MethodReturningFieldAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly IDisposable disposable = new Disposable();

        private object Meh()
        {
            return this.disposable;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void MethodReturningFieldIndexerAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly IDisposable[] disposable = { new Disposable() };

        private object Meh()
        {
            return this.disposable[0];
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void MethodReturningFieldDisposableListIndexerAsObject()
        {
            var disposableListCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class DisposableList<T> : IReadOnlyList<T>, IDisposable
    {
        private readonly List<T> inner = new List<T>();

        public int Count => this.inner.Count;

        public T this[int index] => this.inner[index];

        public IEnumerator<T> GetEnumerator() => this.inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.inner).GetEnumerator();

        public void Add(T item) => this.inner.Add(item);

        public void Dispose()
        {
            this.inner.Clear();
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly DisposableList<IDisposable> disposable = new DisposableList<IDisposable> { new Disposable() };

        private object Meh()
        {
            return this.disposable[0];
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, disposableListCode, testCode);
        }

        [Test]
        public void MethodReturningFieldDisposableListIndexerAsObjectId()
        {
            var disposableListCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class DisposableList<T> : IReadOnlyList<T>, IDisposable
    {
        private readonly List<T> inner = new List<T>();

        public int Count => this.inner.Count;

        public T this[int index] => this.inner[index];

        public IEnumerator<T> GetEnumerator() => this.inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.inner).GetEnumerator();

        public void Add(T item) => this.inner.Add(item);

        public void Dispose()
        {
            this.inner.Clear();
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly DisposableList<IDisposable> disposable = new DisposableList<IDisposable> { new Disposable() };

        private object Meh()
        {
            return this.Id(this.disposable[0]);
        }

        private object Id(object item)
        {
            return item;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, disposableListCode, testCode);
        }

        [Test]
        public void MethodReturningFuncObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static Func<object> Meh()
        {
            return () => new object();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningObjectExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static object Meh() => new object();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PropertyReturningObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            var meh = Meh;
        }

        public object Meh
        {
            get
            {
                return new object();
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IndexerReturningObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            var meh = this[0];
        }

        public object this[int index]
        {
            get
            {
                return new object();
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningTaskFromResultOfDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    public class Foo
    {
        public void Bar()
        {
            CreateDisposableAsync();
        }

        private static Task<IDisposable> CreateDisposableAsync()
        {
            return Task.FromResult<IDisposable>(new Disposable());
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void MethodReturningTaskRunOfDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Threading.Tasks;

    public class Foo
    {
        private static Task<IDisposable> Meh()
        {
            return Task.Run<IDisposable>(() => new Disposable());
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void GenericMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            Id(1);
        }

        private static T Id<T>(T meh)
        {
            return meh;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PropertyReturningObjectExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Bar()
        {
            var meh = Meh;
        }

        public object Meh => new object();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningFileOpenReadAsStream()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public Stream Bar()
        {
            return File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningFileOpenReadExtensionMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public static class Foo
    {
        public static Stream Bar()
        {
            return string.Empty.Bar();
        }

        public static Stream Bar(this string name)
        {
            return File.OpenRead(name);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningNewDisposableExtensionMethodId()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public static class Foo
    {
        public static IDisposable Bar()
        {
            return new Disposable().Id();
        }

        public static IDisposable Id(this IDisposable self)
        {
            return self;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void ReturnDisposableFieldAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public object Meh()
        {
            return stream;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturnDisposableStaticFieldAsObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public sealed class Foo
    {
        private readonly static Stream Stream = File.OpenRead(string.Empty);

        public object Meh()
        {
            return Stream;
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
        public void IEnumerableOfInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections;
    using System.Collections.Generic;

    public class Foo : IEnumerable<int>
    {
        private readonly List<int> ints = new List<int>();

        public IEnumerator<int> GetEnumerator()
        {
            return this.ints.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IEnumerableOfIntSimple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections;
    using System.Collections.Generic;

    public class Foo : IEnumerable
    {
        private readonly List<int> ints = new List<int>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.ints.GetEnumerator();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IEnumerableOfIntExpressionBodies()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections;
    using System.Collections.Generic;

    public class Foo : IEnumerable<int>
    {
        private readonly List<int> ints = new List<int>();

        public IEnumerator<int> GetEnumerator() => this.ints.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningAsyncTaskOfStream()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal static class Foo
    {
        internal static async Task<Stream> ReadAsync(string file)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(file))
            {
                await fileStream.CopyToAsync(stream)
                                .ConfigureAwait(false);
            }

            stream.Position = 0;
            return stream;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void Lambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    internal static class Foo
    {
        internal static void Bar()
        {
            Func<IDisposable> f = () =>
            {
                var file = System.IO.File.OpenRead(null);
                return file;
            };
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PassingFuncToMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo
    {
        public Foo()
        {
            var text = Get(""abc"", 1, (s, i) => s.Substring(i));
        }

        public T Get<T>(T text, int i, Func<T, int, T> meh)
        {
            return meh(text, i);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CallingOverload()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Threading;
    using System.Threading.Tasks;

    public class Foo
    {
        public Task Bar(string source, int expected, CancellationToken cancellationToken)
        {
            return this.Bar(source, new[] { expected }, cancellationToken);
        }

        public Task Bar(string source, int[] expected, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssertThrows()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using NUnit.Framework;

    public class FooTests
    {
        [Test]
        [SuppressMessage(""ReSharper"", ""ObjectCreationAsStatement"")]
        public void ThrowsIfPrerequisiteIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new Disposable());
            Assert.AreEqual(""Value cannot be null.\r\nParameter name: condition2"", exception.Message);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void ReturningDisposedFromUsing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static object Meh()
        {
            using (var disposable = new Disposable())
            {
                return disposable;
            }
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }
    }
}
