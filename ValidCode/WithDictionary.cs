namespace ValidCode
{
    using System.Collections.Generic;
    using System.IO;

    public class WithDictionary
    {
        private readonly Dictionary<int, Stream> streams = new Dictionary<int, Stream>();

        public WithDictionary()
        {
            this.streams[0] = File.OpenRead(string.Empty);
        }

        public Stream Get(int i)
        {
            return this.streams[i];
        }

        public void Set(int i, string fileName)
        {
            if (this.streams.TryGetValue(i, out var stream))
            {
                stream.Dispose();
            }

            this.streams[i] = File.OpenRead(fileName);
        }
    }
}
