namespace ValidCode.Collections
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal sealed class ListOfDisposable : IDisposable
    {
        private readonly List<Stream> _streams = new List<Stream> { null };

        public ListOfDisposable()
        {
            this._streams.Add(File.OpenRead(string.Empty));
        }

        public void Meh()
        {
            this._streams[0].Dispose();
            this._streams[0] = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            foreach (var stream in this._streams)
            {
                stream?.Dispose();
            }

            this._streams.Clear();
        }
    }
}
