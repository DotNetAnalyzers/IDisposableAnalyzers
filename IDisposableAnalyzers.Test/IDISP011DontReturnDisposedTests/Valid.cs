// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test.IDISP011DontReturnDisposedTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class Valid
{
    private static readonly ReturnValueAnalyzer Analyzer = new();

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
        var code = @"
#nullable disable
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
    public static void VoidMethodReturn()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void M1()
        {
            M2();
        }

        private static void M2()
        {
            return;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void MethodReturningObject()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void M1()
        {
            M2();
        }

        private static object M2()
        {
            return new object();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void MethodReturningDynamic()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void M1()
        {
            M2();
        }

        private static dynamic M2()
        {
            return new object();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void GenericClassMethodReturningDynamicSubtract()
    {
        var code = @"
#pragma warning disable CS8600, CS8618, CS0649
namespace N
{
    public class C<T>
    {
        private readonly T item1;
        private readonly T item2;

        private dynamic M() => (dynamic)item1 - (dynamic)item2; //Supersnyggt
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void GenericClassPropertyReturningDynamicSubtract()
    {
        var code = @"
#pragma warning disable CS8600, CS8618, CS0649
namespace N
{
    public class C<T>
    {
        private readonly T item1;
        private readonly T item2;

        private dynamic P => (dynamic)item1 - (dynamic)item2; //Supersnyggt
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void MethodReturningThis()
    {
        var chunkOfT = @"
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

        var code = @"
namespace N
{
    public sealed class C
    {
        public object M()
        {
            var chunk = new Chunk<int>();
            return chunk.Add(1);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, chunkOfT, code);
    }

    [Test]
    public static void MethodReturningFieldAsObject()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable disposable = new Disposable();

        private object M()
        {
            return this.disposable;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [Test]
    public static void MethodReturningFieldIndexerAsObject()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable[] disposable = { new Disposable() };

        private object M()
        {
            return this.disposable[0];
        }
    }
}";
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [Test]
    public static void MethodReturningFieldDisposableListIndexerAsObject()
    {
        var disposableListOfT = @"
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

        var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly DisposableList<IDisposable> disposable = new DisposableList<IDisposable> { new Disposable() };

        private object M()
        {
            return this.disposable[0];
        }
    }
}";
        RoslynAssert.Valid(Analyzer, DisposableCode, disposableListOfT, code);
    }

    [Test]
    public static void MethodReturningFieldDisposableListIndexerAsObjectId()
    {
        var disposableListOfT = @"
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

        var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly DisposableList<IDisposable> disposable = new DisposableList<IDisposable> { new Disposable() };

        private object M()
        {
            return this.Id(this.disposable[0]);
        }

        private object Id(object item)
        {
            return item;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, DisposableCode, disposableListOfT, code);
    }

    [Test]
    public static void MethodReturningFuncObject()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M1()
        {
            M2();
        }

        private static Func<object> M2()
        {
            return () => new object();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void MethodReturningObjectExpressionBody()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void M1()
        {
            M2();
        }

        private static object M2() => new object();
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void PropertyReturningObject()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void M()
        {
            var p = P;
        }

        public object P
        {
            get
            {
                return new object();
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IndexerReturningObject()
    {
        var code = @"
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
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void MethodReturningTaskFromResultOfDisposable()
    {
        var code = @"
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
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [Test]
    public static void MethodReturningTaskRunOfDisposable()
    {
        var code = @"
namespace N
{
    using System;
    using System.Threading.Tasks;

    public class C
    {
        private static Task<IDisposable> M()
        {
            return Task.Run<IDisposable>(() => new Disposable());
        }
    }
}";
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [Test]
    public static void GenericMethod()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void M()
        {
            Id(1);
        }

        private static T Id<T>(T t)
        {
            return t;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void PropertyReturningObjectExpressionBody()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void M()
        {
            var p = P;
        }

        public object P => new object();
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturningFileOpenReadAsStream()
    {
        var code = @"
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
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturningFileOpenReadExtensionMethod()
    {
        var code = @"
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
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturningNewDisposableExtensionMethodId()
    {
        var code = @"
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
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [Test]
    public static void ReturnDisposableFieldAsObject()
    {
        var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public object M()
        {
            return stream;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturnDisposableStaticFieldAsObject()
    {
        var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private readonly static Stream Stream = File.OpenRead(string.Empty);

        public object M()
        {
            return Stream;
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
    public static void IEnumerableOfInt()
    {
        var code = @"
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

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IEnumerableOfIntSimple()
    {
        var code = @"
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

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IEnumerableOfIntExpressionBodies()
    {
        var code = @"
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

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturningAsyncTaskOfStream()
    {
        var code = @"
namespace N
{
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

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void Lambda()
    {
        var code = @"
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
                var file = File.OpenRead(string.Empty);
                return file;
            };
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void PassingFuncToMethod()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        public C()
        {
            var text = Get(""abc"", 1, (s, i) => s.Substring(i));
        }

        public T Get<T>(T text, int i, Func<T, int, T> funk)
        {
            return funk(text, i);
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void CallingOverload()
    {
        var code = @"
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

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void AssertThrows()
    {
        var code = @"
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
            Assert.AreEqual(""Value cannot be null.\r\nParameter name: condition2"", exception?.Message);
        }
    }
}";

        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }

    [Test]
    public static void YieldReturnFromUsing()
    {
        var code = @"
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
                string? line;
                while((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturnGreedyFromUsing()
    {
        var code = @"
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
            string? line;
            while((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            return lines;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void AwaitingInUsing()
    {
        var code = @"
#pragma warning disable SYSLIB0014
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
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void WhenDisposedAndReassigned()
    {
        var code = @"
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
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void WhenDisposedAndReassignedWithLocal()
    {
        var code = @"
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
        RoslynAssert.Valid(Analyzer, code);
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
        public Stream? DisposeBefore(string[] fileNames)
        {
            Stream? stream = null;
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

    [Test]
    public static void DisposedInCatchThenRethrow()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public MemoryStream M()
        {
            var ms = new MemoryStream();
            try
            {
            }
            catch (Exception)
            {
                ms.Dispose();
                throw;
            }

            return ms;
        }
    }
}
";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReturnProcessIssue246()
    {
        var code = @"
#nullable disable
namespace N
{
    using System.Diagnostics;
    using System.Reflection;

    public class C
    {
        public static Process Spawn(bool isNetCoreApp)
        {
            var process = new Process();
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = true;

            if (isNetCoreApp)
            {
                process.StartInfo.FileName = ""dotnet"";
                process.StartInfo.Arguments = Assembly.GetEntryAssembly().Location;
            }
            else
            {
                process.StartInfo.FileName = Assembly.GetEntryAssembly().Location;
            }

            process.Start();
            return process;  // IDISP011	Don't return disposed instance
        }
    }
}
";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IfDisposeAssignNewIssue239()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        IDisposable M1(bool b)
        {
            IDisposable value = new Foo();

            if (b)
            {
                value.Dispose();
                value = new Bar();
            }

            return value;
        }

        sealed class Foo : IDisposable
        {
            public void Dispose()
            {
            }
        }

        sealed class Bar : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
";
        RoslynAssert.Valid(Analyzer, code);
    }
}
