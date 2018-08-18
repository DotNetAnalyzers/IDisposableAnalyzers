namespace ValidCode
{
    using System.Collections.Concurrent;
    using System.IO;

    internal class Caching
    {
        private static readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();
        private readonly ConcurrentDictionary<int, Stream> cache = new ConcurrentDictionary<int, Stream>();

        public static long Bar()
        {
            var stream = Cache.GetOrAdd(1, _ => File.OpenRead(string.Empty));
            return stream.Length;
        }

        public long Bar1()
        {
            var stream = this.cache.GetOrAdd(1, _ => File.OpenRead(string.Empty));
            return stream.Length;
        }
    }
}
