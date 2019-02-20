namespace ValidCode
{
    using System.IO;

    public class WhenCreatingAndDisposing
    {
        public Stream Stream => File.OpenRead(string.Empty);

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
