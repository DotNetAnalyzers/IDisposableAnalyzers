namespace ValidCode
{
    using System.IO;

    public class ReassigningField
    {
        private Stream stream;

        public void DisposeAndReassign()
        {
            this.stream.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }

        public void ConditionalDisposeAndReassign()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }
}
