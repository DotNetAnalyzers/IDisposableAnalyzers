// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP005ReturntypeShouldIndicateIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP005ReturntypeShouldIndicateIDisposable>
    {
        private static readonly string DisposableCode = @"
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
        public async Task RealisticExtensionMethodClass()
        {
            var testCode = @"
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
    }";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task VoidMethodReturn()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningObject()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningDynamic()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task GenericClassMethodReturningDynamicSubtract()
        {
            var testCode = @"
    public class Foo<T>
    {
        private readonly T item1;
        private readonly T item2;

        private dynamic Meh() => (dynamic)item1 - (dynamic)item2; //Supersnyggt
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task GenericClassPropertyReturningDynamicSubtract()
        {
            var testCode = @"
    public class Foo<T>
    {
        private readonly T item1;
        private readonly T item2;

        private dynamic Meh => (dynamic)item1 - (dynamic)item2; //Supersnyggt
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningThis()
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
            await this.VerifyHappyPathAsync(chunkCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningFieldAsObject()
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
            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningFieldIndexerAsObject()
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
            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningFieldDisposableListIndexerAsObject()
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
            await this.VerifyHappyPathAsync(DisposableCode, disposableListCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningFieldDisposableListIndexerAsObjectId()
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
            await this.VerifyHappyPathAsync(DisposableCode, disposableListCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningFuncObject()
        {
            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningObjectExpressionBody()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static object Meh() => new object();
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyReturningObject()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IndexerReturningObject()
        {
            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningTaskFromResultOfDisposable()
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
            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningTaskRunOfDisposable()
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
            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task GenericMethod()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyReturningObjectExpressionBody()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            var meh = Meh;
        }

        public object Meh => new object();
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningFileOpenReadAsStream()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public Stream Bar()
        {
            return File.OpenRead(string.Empty);
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningFileOpenReadExtensionMethod()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningNewDisposableExtensionMethodId()
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
            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnDisposableFieldAsObject()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    private readonly Stream stream = File.OpenRead(string.Empty);

    public object Meh()
    {
        return stream;
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnDisposableStaticFieldAsObject()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    private readonly static Stream Stream = File.OpenRead(string.Empty);

    public object Meh()
    {
        return Stream;
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IfTry()
        {
            var testCode = @"
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
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IEnumerableOfInt()
        {
            var testCode = @"
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
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IEnumerableOfIntSimple()
        {
            var testCode = @"
using System.Collections;
using System.Collections.Generic;

public class Foo : IEnumerable
{
    private readonly List<int> ints = new List<int>();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.ints.GetEnumerator();
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IEnumerableOfIntExpressionBodies()
        {
            var testCode = @"
using System.Collections;
using System.Collections.Generic;

public class Foo : IEnumerable<int>
{
    private readonly List<int> ints = new List<int>();

    public IEnumerator<int> GetEnumerator() => this.ints.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningAsyncTaskOfStream()
        {
            var testCode = @"
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
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Lambda()
        {
            var testCode = @"
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
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task PassingFuncToMethod()
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

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task CallingOverload()
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

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task AssertThrows()
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

            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningDisposedFromUsing()
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

            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }
    }
}