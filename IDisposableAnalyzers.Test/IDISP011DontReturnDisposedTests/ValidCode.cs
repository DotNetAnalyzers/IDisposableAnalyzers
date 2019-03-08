// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP011DontReturnDisposedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ReturnValueAnalyzer();

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
        public void VoidMethodReturn()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningObject()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningDynamic()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void GenericClassMethodReturningDynamicSubtract()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C<T>
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
    public class C<T>
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
    public sealed class C
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

    public class C
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

    public class C
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

    public class C
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
            AnalyzerAssert.Valid(Analyzer, DisposableCode, disposableListCode, testCode);
        }

        [Test]
        public void MethodReturningFuncObject()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MethodReturningObjectExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PropertyReturningObject()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IndexerReturningObject()
        {
            var testCode = @"
namespace RoslynSandbox
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

    public class C
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PropertyReturningObjectExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningFileOpenReadAsStream()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningFileOpenReadExtensionMethod()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningNewDisposableExtensionMethodId()
        {
            var testCode = @"
namespace RoslynSandbox
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

    public sealed class C
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

    public sealed class C
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

    public class C : IEnumerable
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

    public class C : IEnumerable<int>
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

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PassingFuncToMethod()
        {
            var testCode = @"
namespace RoslynSandbox
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

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void YieldReturnFromUsing()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturnGreedyFromUsing()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AwaitingInUsing()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenDisposedAndReassigned()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenDisposedAndReassignedWithLocal()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposedBeforeInForeach()
        {
            var code = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, code);
        }
    }
}
