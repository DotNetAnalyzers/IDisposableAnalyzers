// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    internal static class KnownSymbols
    {
        internal static readonly QualifiedType Void = Create("System.Void", "void");
        internal static readonly QualifiedType Object = Create("System.Object", "object");
        internal static readonly QualifiedType Boolean = Create("System.Boolean", "bool");
        internal static readonly QualifiedType Activator = Create("System.Activator");
        internal static readonly QualifiedType Func = Create("System.Func");
        internal static readonly QualifiedType BinaryReader = Create("System.IO.BinaryReader");
        internal static readonly QualifiedType BinaryWriter = Create("System.IO.BinaryWriter");
        internal static readonly QualifiedType StreamReader = Create("System.IO.StreamReader");
        internal static readonly QualifiedType StreamWriter = Create("System.IO.StreamWriter");
        internal static readonly QualifiedType File = new("System.IO.File");
        internal static readonly QualifiedType FileInfo = new("System.IO.FileInfo");
        internal static readonly QualifiedType CryptoStream = Create("System.Security.Cryptography.CryptoStream");
        internal static readonly QualifiedType DeflateStream = Create("System.IO.Compression.DeflateStream");
        internal static readonly QualifiedType GZipStream = Create("System.IO.Compression.GZipStream");
        internal static readonly QualifiedType StreamMemoryBlockProvider = Create("System.Reflection.Internal.StreamMemoryBlockProvider");
        internal static readonly QualifiedType ECDsaCng = Create("System.Security.Cryptography.ECDsaCng");
        internal static readonly QualifiedType ConstructorInfo = Create("System.Reflection.ConstructorInfo");
        internal static readonly QualifiedType Attachment = Create("System.Net.Mail.Attachment");

        internal static readonly TupleType Tuple = new();
        internal static readonly IDisposableType IDisposable = new();
        internal static readonly IAsyncDisposableType IAsyncDisposable = new();
        internal static readonly GCType GC = new();
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

        internal static readonly ResourceManagerType ResourceManager = new();
        internal static readonly QualifiedType RegistryKey = new("Microsoft.Win32.RegistryKey");
        internal static readonly IEnumerableType IEnumerable = new();
        internal static readonly IEnumerableOfTType IEnumerableOfT = new();
        internal static readonly QualifiedType IEnumerator = new("System.Collections.IEnumerator");
        internal static readonly EnumerableType Enumerable = new();
        internal static readonly QualifiedType ConditionalWeakTable = Create("System.Runtime.CompilerServices.ConditionalWeakTable`2");
        internal static readonly TaskType Task = new();
        internal static readonly QualifiedType ValueTaskOfT = new("System.Threading.Tasks.ValueTask`1");
        internal static readonly QualifiedType TaskOfT = new("System.Threading.Tasks.Task`1");
        internal static readonly QualifiedType CancellationToken = new("System.Threading.CancellationToken");
        internal static readonly QualifiedType Interlocked = new("System.Threading.Interlocked");
        internal static readonly QualifiedType INotifyCompletion = new("System.Runtime.CompilerServices.INotifyCompletion");
        internal static readonly QualifiedType HttpClient = new("System.Net.Http.HttpClient");
        internal static readonly QualifiedType HttpMessageHandler = new("System.Net.Http.HttpMessageHandler");
        internal static readonly HttpResponseType HttpResponse = new();
        internal static readonly HttpResponseMessageType HttpResponseMessage = new();

        internal static readonly SerialDisposableType SerialDisposable = new();
        internal static readonly RxDisposableType RxDisposable = new();
        internal static readonly QualifiedType RxIScheduler = new("System.Reactive.Concurrency.IScheduler");
        internal static readonly SingleAssignmentDisposableType SingleAssignmentDisposable = new();
        internal static readonly CompositeDisposableType CompositeDisposable = new();

        internal static readonly PasswordBoxType PasswordBox = new();
        internal static readonly QualifiedType SystemWindowsFormsForm = new("System.Windows.Forms.Form");
        internal static readonly WinformsControlType SystemWindowsFormsControl = new();
        internal static readonly HostingAbstractionsHostExtensionsType HostingAbstractionsHostExtensions = new();

        internal static readonly QualifiedType TestInitializeAttribute = new("Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute");
        internal static readonly QualifiedType ClassInitializeAttribute = new("Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute");
        internal static readonly QualifiedType TestCleanupAttribute = new("Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute");
        internal static readonly QualifiedType ClassCleanupAttribute = new("Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute");

        internal static readonly QualifiedType NinjectStandardKernel = new("Ninject.StandardKernel");
        internal static readonly QualifiedType ILoggerFactory = new("Microsoft.Extensions.Logging.ILoggerFactory");
        internal static readonly IHostedServiceType IHostedService = new();
        internal static readonly DisposableMixins DisposableMixins = new();

        private static QualifiedType Create(string qualifiedName, string? alias = null)
        {
            return new(qualifiedName, alias);
        }

        internal static class NUnit
        {
            internal static readonly QualifiedType Assert = new("NUnit.Framework.Assert");
            internal static readonly QualifiedType SetUpAttribute = new("NUnit.Framework.SetUpAttribute");
            internal static readonly QualifiedType TearDownAttribute = new("NUnit.Framework.TearDownAttribute");
            internal static readonly QualifiedType OneTimeSetUpAttribute = new("NUnit.Framework.OneTimeSetUpAttribute");
            internal static readonly QualifiedType OneTimeTearDownAttribute = new("NUnit.Framework.OneTimeTearDownAttribute");
        }
    }
}
