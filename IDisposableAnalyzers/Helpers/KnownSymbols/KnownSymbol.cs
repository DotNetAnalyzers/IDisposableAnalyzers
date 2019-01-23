// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    internal static class KnownSymbol
    {
        internal static readonly QualifiedType Void = Create("System.Void", "void");
        internal static readonly QualifiedType Object = Create("System.Object", "object");
        internal static readonly QualifiedType Boolean = Create("System.Boolean", "bool");
        internal static readonly QualifiedType Func = Create("System.Func");
        internal static readonly DisposableType IDisposable = new DisposableType();
        internal static readonly GCType GC = new GCType();
        internal static readonly QualifiedType IDictionary = Create("System.Collections.IDictionary");

        internal static readonly QualifiedType ListOfT = Create("System.Collections.Generic.List`1");
        internal static readonly QualifiedType StackOfT = Create("System.Collections.Generic.Stack`1");
        internal static readonly QualifiedType QueueOfT = Create("System.Collections.Generic.Queue`1");
        internal static readonly QualifiedType LinkedListOfT = Create("System.Collections.Generic.LinkedList`1");
        internal static readonly QualifiedType SortedSetOfT = Create("System.Collections.Generic.SortedSet`1");

        internal static readonly QualifiedType DictionaryOfTKeyTValue = Create("System.Collections.Generic.Dictionary`2");
        internal static readonly QualifiedType SortedListOfTKeyTValue = Create("System.Collections.Generic.SortedList`2");
        internal static readonly QualifiedType SortedDictionaryOfTKeyTValue = Create("System.Collections.Generic.SortedDictionary`2");

        internal static readonly QualifiedType ImmutableHashSetOfT = Create("System.Collections.Immutable.ImmutableHashSet`1");
        internal static readonly QualifiedType ImmutableListOfT = Create("System.Collections.Immutable.ImmutableList`1");
        internal static readonly QualifiedType ImmutableQueueOfT = Create("System.Collections.Immutable.ImmutableQueue`1");
        internal static readonly QualifiedType ImmutableSortedSetOfT = Create("System.Collections.Immutable.ImmutableSortedSet`1");
        internal static readonly QualifiedType ImmutableStackOfT = Create("System.Collections.Immutable.ImmutableStack`1");

        internal static readonly QualifiedType ImmutableDictionaryOfTKeyTValue = Create("System.Collections.Immutable.ImmutableDictionary`2");
        internal static readonly QualifiedType ImmutableSortedDictionaryOfTKeyTValue = Create("System.Collections.Immutable.ImmutableSortedDictionary`2");

        internal static readonly ResourceManagerType ResourceManager = new ResourceManagerType();

        internal static readonly FileType File = new FileType();
        internal static readonly IEnumerableType IEnumerable = new IEnumerableType();
        internal static readonly IEnumerableOfTType IEnumerableOfT = new IEnumerableOfTType();
        internal static readonly QualifiedType IEnumerator = new QualifiedType("System.Collections.IEnumerator");
        internal static readonly EnumerableType Enumerable = new EnumerableType();
        internal static readonly QualifiedType ConditionalWeakTable = Create("System.Runtime.CompilerServices.ConditionalWeakTable`2");
        internal static readonly TaskType Task = new TaskType();
        internal static readonly QualifiedType TaskOfT = new QualifiedType("System.Threading.Tasks.Task`1");
        internal static readonly QualifiedType HttpClient = new QualifiedType("System.Net.Http.HttpClient");
        internal static readonly HttpResponseMessageType HttpResponseMessage = new HttpResponseMessageType();

        internal static readonly SerialDisposableType SerialDisposable = new SerialDisposableType();
        internal static readonly RxDisposableType RxDisposable = new RxDisposableType();
        internal static readonly SingleAssignmentDisposableType SingleAssignmentDisposable = new SingleAssignmentDisposableType();
        internal static readonly CompositeDisposableType CompositeDisposable = new CompositeDisposableType();

        internal static readonly PasswordBoxType PasswordBox = new PasswordBoxType();
        internal static readonly QualifiedType NUnitSetUpAttribute = new QualifiedType("NUnit.Framework.SetUpAttribute");
        internal static readonly QualifiedType NUnitTearDownAttribute = new QualifiedType("NUnit.Framework.TearDownAttribute");
        internal static readonly QualifiedType NUnitOneTimeSetUpAttribute = new QualifiedType("NUnit.Framework.OneTimeSetUpAttribute");
        internal static readonly QualifiedType NUnitOneTimeTearDownAttribute = new QualifiedType("NUnit.Framework.OneTimeTearDownAttribute");
        internal static readonly QualifiedType NinjectStandardKernel = new QualifiedType("Ninject.StandardKernel");
        internal static readonly QualifiedType ILoggerFactory = new QualifiedType("Microsoft.Extensions.Logging.ILoggerFactory");

        private static QualifiedType Create(string qualifiedName, string alias = null)
        {
            return new QualifiedType(qualifiedName, alias);
        }
    }
}
