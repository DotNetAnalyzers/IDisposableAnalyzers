namespace ValidCode
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal sealed class ListOfDisposable : IDisposable
    {
        private readonly List<Stream> _streams = new List<Stream> { null };

        public ListOfDisposable()
        {
            _streams.Add(File.OpenRead(string.Empty));
        }

        public void Meh()
        {
            _streams[0].Dispose();
            _streams[0] = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            foreach (var stream in _streams)
            {
                stream?.Dispose();
            }

            _streams.Clear();
        }
    }
}
