namespace ValidCode
{
    using System;
    using System.IO;

    public sealed class ReassigningField : IDisposable
    {
        private Stream? stream;

        public void DisposeAndReassign()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            this.stream.Dispose();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            this.stream = File.OpenRead(string.Empty);
        }

        public void ConditionalDisposeAndReassign()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
        }
    }
}
