namespace ValidCode
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal sealed class ListOfDisposable : IDisposable
    {
        private List<Stream> _streams = new List<Stream> { null };

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
