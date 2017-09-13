namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP001DisposeCreated>
    {
        public class Cached : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task DontUseUsingWhenGettingFromStaticFieldConcurrentDictionaryGetOrAdd()
            {
                var testCode = @"
using System.Collections.Concurrent;
using System.IO;

public static class Foo
{
    private static readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public static long Bar()
    {
        var stream = Cache.GetOrAdd(1, _ => File.OpenRead(string.Empty));
        return stream.Length;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenGettingFromFieldConcurrentDictionaryGetOrAdd()
            {
                var testCode = @"
using System.Collections.Concurrent;
using System.IO;

public class Foo
{
    private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public long Bar()
    {
        var stream = Cache.GetOrAdd(1, _ => File.OpenRead(string.Empty));
        return stream.Length;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenGettingFromConcurrentDictionaryTryGetValue()
            {
                var testCode = @"
using System.Collections.Concurrent;
using System.IO;

public static class Foo
{
    private static readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public static long Bar()
    {
        Stream stream;
        if (Cache.TryGetValue(1, out stream))
        {
            return stream.Length;
        }

        return 0;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenGettingFromConditionalWeakTableTryGetValue()
            {
                var testCode = @"
using System.IO;
using System.Runtime.CompilerServices;

public static class Foo
{
    private static readonly ConditionalWeakTable<string, Stream> Cache = new ConditionalWeakTable<string, Stream>();

    public static long Bar()
    {
        Stream stream;
        if (Cache.TryGetValue(""1"", out stream))
        {
            return stream.Length;
        }

        return 0;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}