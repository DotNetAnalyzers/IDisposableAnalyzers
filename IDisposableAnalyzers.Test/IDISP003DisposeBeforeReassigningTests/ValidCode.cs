namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(ArgumentAnalyzer))]
    [TestFixture(typeof(AssignmentAnalyzer))]
    public partial class ValidCode<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly T Analyzer = new T();

#pragma warning disable SA1203 // Constants must appear before fields
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
        public void LocalDeclaration()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalAssignedInSwitch()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public static class Foo
    {
        public static IDisposable Bar(int i)
        {
            IDisposable result;
            switch (i)
            {
                case 1:
                    result = File.OpenRead(string.Empty);
                    break;
                case 2:
                    result = File.OpenRead(string.Empty);
                    break;
                default:
                    result = null;
                    break;
            }

            return result;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalAssignedInIfElseSwitch()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public static class Foo
    {
        public static IDisposable Bar(int i)
        {
            IDisposable result;
            if (i == 0)
            {
                result = null;
            }
            else
            {
                switch (i)
                {
                    case 1:
                        result = File.OpenRead(string.Empty);
                        break;
                    case 2:
                        result = File.OpenRead(string.Empty);
                        break;
                    default:
                        result = null;
                        break;
                }
            }

            return result;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssignVariableInitializedWithNull()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream = null;
            stream = File.OpenRead(string.Empty);
            stream.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("(stream as IDisposable)?.Dispose()")]
        [TestCase("(stream as IDisposable).Dispose()")]
        [TestCase("((IDisposable)stream).Dispose()")]
        [TestCase("((IDisposable)stream)?.Dispose()")]
        public void NotDisposingVariableOfTypeObject(string disposeCode)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            object stream = File.OpenRead(string.Empty);
            (stream as IDisposable)?.Dispose();
            stream = File.OpenRead(string.Empty);
            (stream as IDisposable)?.Dispose();
        }
    }
}".AssertReplace("(stream as IDisposable)?.Dispose()", disposeCode);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningPropertyInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningPropertyInCtorInDisposableType()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream { get; }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningIndexerInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        private readonly List<int> ints = new List<int>();

        public Foo()
        {
            this[1] = 1;
        }

        public int this[int index]
        {
            get { return this.ints[index]; }
            set { this.ints[index] = value; }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningIndexerInCtorInDisposableType()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo : IDisposable
    {
        private readonly List<int> ints = new List<int>();

        public Foo()
        {
            this[1] = 1;
        }

        public int this[int index]
        {
            get { return this.ints[index]; }
            set { this.ints[index] = value; }
        }

        public void Dispose()
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningPropertyWithBackingFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            set { this.stream = value; }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningFieldInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private readonly Stream stream;

        public Foo()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void FieldSwapCached()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.IO;

    public class Foo
    {
        private readonly Dictionary<int, Stream> Cache = new Dictionary<int, Stream>();

        private Stream current;

        public void SetCurrent(int number)
        {
            this.current = this.Cache[number];
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LocalSwapCached()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.IO;

    public class Foo
    {
        private readonly Dictionary<int, Stream> Cache = new Dictionary<int, Stream>();

        public void SetCurrent(int number)
        {
            var current = this.Cache[number];
            current = this.Cache[number + 1];
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

    public class Foo
    {
        private readonly DisposableDictionary<int, Stream> Cache = new DisposableDictionary<int, Stream>();

        public void SetCurrent(int number)
        {
            var current = this.Cache[number];
            current = this.Cache[number + 1];
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, disposableDictionaryCode, testCode);
        }

        [Test]
        public void LocalSwapCachedTryGetValue()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.IO;

    public class Foo
    {
        private readonly Dictionary<int, Stream> Cache = new Dictionary<int, Stream>();

        public void SetCurrent(int number)
        {
            Stream current = this.Cache[number];
            this.Cache.TryGetValue(1, out current);
            Stream temp;
            this.Cache.TryGetValue(2, out temp);
            current = temp;
            current = this.Cache[number + 1];
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssigningInIfElse()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        public void Meh(bool b)
        {
            Stream stream;
            if (b)
            {
                stream = File.OpenRead(string.Empty);
            }
            else
            {
                stream = File.OpenRead(string.Empty);
            }

            stream?.Dispose();
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void OutParameterInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void OutParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("Stream stream;")]
        [TestCase("Stream stream = null;")]
        [TestCase("var stream = (Stream)null;")]
        public void VariableSplitDeclarationAndAssignment(string declaration)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            Stream stream;
            stream = File.OpenRead(string.Empty);
            stream.Dispose();
        }
    }
}";

            testCode = testCode.AssertReplace("Stream stream;", declaration);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WithOptionalParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        private IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = Bar(disposable);
        }

        private static IDisposable Bar(IDisposable disposable, IEnumerable<IDisposable> disposables = null)
        {
            if (disposables == null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ChainedCalls()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        private IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = Bar(disposable);
        }

        private static IDisposable Bar(IDisposable disposable)
        {
            if (disposable == null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }

        private static IDisposable Bar(IDisposable disposable, IDisposable[] list)
        {
            return disposable;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ChainedCallsWithHelper()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = Helper.Bar(disposable);
        }
    }
}";

            var helperCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public static class Helper
    {
        public static IDisposable Bar(IDisposable disposable)
        {
            if (disposable == null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }

        public static IDisposable Bar(IDisposable disposable, IDisposable[] list)
        {
            return disposable;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, helperCode, testCode);
        }

        [Test]
        public void ReproIssue71()
        {
            var code = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxonomyWpf
{
    public class IndexedList<T> : IList<KeyValuePair<int, T>>
    {
        protected IList<T> decorated;

        public IndexedList(IList<T> decorated)
        {
            if(decorated == null)
                throw new ArgumentNullException(nameof(decorated));

            this.decorated = decorated;
        }

        public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
        {
            return decorated.Select((element, index) => new KeyValuePair<int, T>(index, element)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<KeyValuePair<int, T>>.Add(KeyValuePair<int, T> item)
        {
            Add(item.Value);
        }

        public void Add(T item)
        {
            decorated.Add(item);
        }

        public void Clear()
        {
            decorated.Clear();
        }

        bool ICollection<KeyValuePair<int, T>>.Contains(KeyValuePair<int, T> item)
        {
            return Contains(item.Value);
        }

        public bool Contains(T item)
        {
            return decorated.Contains(item);
        }

        public void CopyTo(KeyValuePair<int, T>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<int, T> item)
        {
            return decorated.Remove(item.Value);
        }

        public int Count => decorated.Count;
        public bool IsReadOnly => decorated.IsReadOnly;

        public int IndexOf(KeyValuePair<int, T> item)
        {
            return decorated.IndexOf(item.Value);
        }

        void IList<KeyValuePair<int, T>>.Insert(int index, KeyValuePair<int, T> item)
        {
            Insert(index, item.Value);
        }

        public void Insert(int index, T item)
        {
            decorated.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            decorated.RemoveAt(index);
        }
        public KeyValuePair<int, T> this[int index]
        {
            get { return new KeyValuePair<int, T>(index, decorated[index]); }
            set { decorated[index] = value.Value; }
        }
    }

    public class ObservableIndexedList<T> : IndexedList<T>, INotifyCollectionChanged
    {
        public ObservableIndexedList(ObservableCollection<T> decorated) : 
            base(decorated)
        {

        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { ((ObservableCollection<T>)decorated).CollectionChanged += value; }
            remove { ((ObservableCollection<T>)decorated).CollectionChanged -= value; }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void DisposingBackingFieldInSetter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public Foo()
        {
            this.Stream = File.OpenRead(string.Empty);
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set 
            { 
                this.stream?.Dispose();
                this.stream = value; 
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void LazyProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private IDisposable disposable;
        private bool disposed;

        public IDisposable Disposable => this.disposable ?? (this.disposable = new Disposable());

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.disposable?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void LazyAssigningSingleAssignmentDisposable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly Lazy<int> lazy;
        private readonly SingleAssignmentDisposable disposable = new SingleAssignmentDisposable();

        public Foo(IObservable<object> observable)
        {
            this.lazy = new Lazy<int>(
                () =>
                {
                    disposable.Disposable = new Disposable();
                    return 1;
                });
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void SeparateDeclarationAndAssignment()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            IDisposable disposable;
            disposable = new Disposable();
            disposable.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void SeparateDeclarationAndAssignmentInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                IDisposable disposable;
                disposable = new Disposable();
                disposable.Dispose();
            };
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void SeparateDeclarationAndAssignmentInUsing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void SingleSimpleAssignment()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo()
        {
            this.disposable = new Disposable();
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssigningWithAssignment()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo()
        {
            this.DataContext = disposable = new Disposable();
        }

        public object DataContext { get; set; }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void TryWithEarlyReturn()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class Foo
    {
        private static bool TryGetStream(string fileName, out Stream stream)
        {
            if (File.Exists(fileName))
            {
                stream = File.OpenRead(fileName);
                return true;
            }

            stream = null;
            return false;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingListContent()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class Foo
    {
        private List<Stream> streams = new List<Stream>();

        public void Meh()
        {
            this.streams[0].Dispose();
            this.streams[0] = File.OpenRead(string.Empty);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposingListContentUnderscore()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class Foo
    {
        private List<Stream> _streams = new List<Stream>();

        public void Meh()
        {
            _streams[0].Dispose();
            _streams[0] = File.OpenRead(string.Empty);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturningOutParameterInForeach()
        {
            var code = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public static bool TryGetStream(string[] fileNames, out Stream result)
        {
            foreach (var name in fileNames)
            {
                if (name.Length > 5)
                {
                    result = File.OpenRead(name);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void ReturningOutParameterInFor()
        {
            var code = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public static bool TryGetStreamFor(string[] fileNames, out Stream result)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                string name = fileNames[i];
                if (name.Length > 5)
                {
                    result = File.OpenRead(name);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void ReturningOutParameterInWhile()
        {
            var code = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public static bool TryGetStreamWhile(string[] fileNames, out Stream result)
        {
            var i = 0;
            while (i < fileNames.Length)
            {
                string name = fileNames[i];
                if (name.Length > 5)
                {
                    result = File.OpenRead(name);
                    return true;
                }

                i++;
            }

            result = null;
            return false;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void DisposedAfterInForeach()
        {
            var code = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public void DisposeAfter(string[] fileNames)
        {
            Stream stream = null;
            foreach (var name in fileNames)
            {
                stream = File.OpenRead(name);
                stream.Dispose();
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }
    }
}
