// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP011DontReturnDisposedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();

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

        [Test]
        public static void RealisticExtensionMethodClass()
        {
            var testCode = @"
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void VoidMethodReturn()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Meh();
        }

        private static void Meh()
        {
            return;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void MethodReturningObject()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Meh();
        }

        private static object Meh()
        {
            return new object();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void MethodReturningDynamic()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Meh();
        }

        private static dynamic Meh()
        {
            return new object();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void GenericClassMethodReturningDynamicSubtract()
        {
            var testCode = @"
namespace N
{
    public class C<T>
    {
        private readonly T item1;
        private readonly T item2;

        private dynamic Meh() => (dynamic)item1 - (dynamic)item2; //Supersnyggt
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void GenericClassPropertyReturningDynamicSubtract()
        {
            var testCode = @"
namespace N
{
    public class C<T>
    {
        private readonly T item1;
        private readonly T item2;

        private dynamic Meh => (dynamic)item1 - (dynamic)item2; //Supersnyggt
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void MethodReturningThis()
        {
            var chunkCode = @"
namespace N
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
namespace N
{
    public sealed class C
    {
        public object Meh()
        {
            var chunk = new Chunk<int>();
            return chunk.Add(1);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, chunkCode, testCode);
        }

        [Test]
        public static void MethodReturningFieldAsObject()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable disposable = new Disposable();

        private object Meh()
        {
            return this.disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void MethodReturningFieldIndexerAsObject()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable[] disposable = { new Disposable() };

        private object Meh()
        {
            return this.disposable[0];
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void MethodReturningFieldDisposableListIndexerAsObject()
        {
            var disposableListCode = @"
namespace N
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
namespace N
{
    using System;

    public class C
    {
        private readonly DisposableList<IDisposable> disposable = new DisposableList<IDisposable> { new Disposable() };

        private object Meh()
        {
            return this.disposable[0];
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, disposableListCode, testCode);
        }

        [Test]
        public static void MethodReturningFieldDisposableListIndexerAsObjectId()
        {
            var disposableListCode = @"
namespace N
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
namespace N
{
    using System;

    public class C
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
            RoslynAssert.Valid(Analyzer, DisposableCode, disposableListCode, testCode);
        }

        [Test]
        public static void MethodReturningFuncObject()
        {
            var testCode = @"
namespace N
{
    using System;

    public class C
    {
        public void M()
        {
            Meh();
        }

        private static Func<object> Meh()
        {
            return () => new object();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void MethodReturningObjectExpressionBody()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Meh();
        }

        private static object Meh() => new object();
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void PropertyReturningObject()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IndexerReturningObject()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void MethodReturningTaskFromResultOfDisposable()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Threading.Tasks;

    public class C
    {
        public void M()
        {
            CreateDisposableAsync();
        }

        private static Task<IDisposable> CreateDisposableAsync()
        {
            return Task.FromResult<IDisposable>(new Disposable());
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void MethodReturningTaskRunOfDisposable()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Threading.Tasks;

    public class C
    {
        private static Task<IDisposable> Meh()
        {
            return Task.Run<IDisposable>(() => new Disposable());
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void GenericMethod()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Id(1);
        }

        private static T Id<T>(T meh)
        {
            return meh;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void PropertyReturningObjectExpressionBody()
        {
            var testCode = @"
namespace N
{
    public class C
    {
        public void M()
        {
            var meh = Meh;
        }

        public object Meh => new object();
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningFileOpenReadAsStream()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public class C
    {
        public Stream M()
        {
            return File.OpenRead(string.Empty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningFileOpenReadExtensionMethod()
        {
            var testCode = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static Stream M()
        {
            return string.Empty.M();
        }

        public static Stream M(this string name)
        {
            return File.OpenRead(name);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningNewDisposableExtensionMethodId()
        {
            var testCode = @"
namespace N
{
    using System;

    public static class C
    {
        public static IDisposable M()
        {
            return new Disposable().Id();
        }

        public static IDisposable Id(this IDisposable self)
        {
            return self;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void ReturnDisposableFieldAsObject()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public object Meh()
        {
            return stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturnDisposableStaticFieldAsObject()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly static Stream Stream = File.OpenRead(string.Empty);

        public object Meh()
        {
            return Stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IfTry()
        {
            var testCode = @"
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IEnumerableOfInt()
        {
            var testCode = @"
namespace N
{
    using System.Collections;
    using System.Collections.Generic;

    public class C : IEnumerable<int>
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IEnumerableOfIntSimple()
        {
            var testCode = @"
namespace N
{
    using System.Collections;
    using System.Collections.Generic;

    public class C : IEnumerable
    {
        private readonly List<int> ints = new List<int>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.ints.GetEnumerator();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IEnumerableOfIntExpressionBodies()
        {
            var testCode = @"
namespace N
{
    using System.Collections;
    using System.Collections.Generic;

    public class C : IEnumerable<int>
    {
        private readonly List<int> ints = new List<int>();

        public IEnumerator<int> GetEnumerator() => this.ints.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturningAsyncTaskOfStream()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal static class C
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void Lambda()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    internal static class C
    {
        internal static void M()
        {
            Func<IDisposable> f = () =>
            {
                var file = System.IO.File.OpenRead(null);
                return file;
            };
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void PassingFuncToMethod()
        {
            var testCode = @"
namespace N
{
    using System;

    public sealed class C
    {
        public C()
        {
            var text = Get(""abc"", 1, (s, i) => s.Substring(i));
        }

        public T Get<T>(T text, int i, Func<T, int, T> meh)
        {
            return meh(text, i);
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void CallingOverload()
        {
            var testCode = @"
namespace N
{
    using System.Threading;
    using System.Threading.Tasks;

    public class C
    {
        public Task M(string source, int expected, CancellationToken cancellationToken)
        {
            return this.M(source, new[] { expected }, cancellationToken);
        }

        public Task M(string source, int[] expected, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AssertThrows()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using NUnit.Framework;

    public class CTests
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

            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public static void YieldReturnFromUsing()
        {
            var testCode = @"
namespace N
{
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        public IEnumerable<string> F()
        {
            using(var reader = File.OpenText(string.Empty))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ReturnGreedyFromUsing()
        {
            var testCode = @"
namespace N
{
    using System.Collections.Generic;
    using System.IO;

    public class C
    {
        public IEnumerable<string> F()
        {
            using (var streamReader = File.OpenText(string.Empty))
            {
                return Use(streamReader);
            }
        }

        IEnumerable<string> Use(TextReader reader)
        {
            List<string> lines = new List<string>();
            string line;
            while((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            return lines;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AwaitingInUsing()
        {
            var testCode = @"
namespace N
{
    using System.Net;
    using System.Threading.Tasks;

    public class C
    {
        public async Task<string> M()
        {
            using (var client = new WebClient())
            {
                return await client.DownloadStringTaskAsync(string.Empty);
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void WhenDisposedAndReassigned()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public IDisposable M(string fileName)
        {
            var x = File.OpenRead(fileName);
            x.Dispose();
            x = File.OpenRead(fileName);
            return x;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void WhenDisposedAndReassignedWithLocal()
        {
            var testCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public IDisposable M(string fileName)
        {
            var x = File.OpenRead(fileName);
            var y = File.OpenRead(fileName);
            x.Dispose();
            x = y;
            return x;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void DisposedBeforeInForeach()
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public Stream DisposeBefore(string[] fileNames)
        {
            Stream stream = null;
            foreach (var name in fileNames)
            {
                stream?.Dispose();
                stream = File.OpenRead(name);
            }

            return stream;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
