namespace ValidCode
{
    using System.IO;

    public class WhenCreatingAndDisposing
    {
#pragma warning disable IDISP012 // Property should not return created disposable.
        public Stream Stream => File.OpenRead(string.Empty);
#pragma warning restore IDISP012 // Property should not return created disposable.

        public Stream GetStream() => File.OpenRead(string.Empty);

        public void M()
        {
            File.OpenRead(string.Empty).Dispose();
            File.OpenRead(string.Empty)?.Dispose();
            this.Stream.Dispose();
            this.Stream?.Dispose();
            this.GetStream().Dispose();
            this.GetStream()?.Dispose();
        }
    }
}
