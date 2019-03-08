// ReSharper disable All
namespace IDisposableAnalyzers.Test.Sandbox
{
    using System.IO;

    public sealed class C
    {
        private static readonly Stream StaticStream = File.OpenRead(string.Empty);
        private Stream stream;

        public C(Stream stream)
        {
            this.stream = stream;
            this.stream = StaticStream;
            this.Stream = stream;
            this.Stream = StaticStream;
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}
