namespace ValidCode.Tuples
{
    using System;
    using System.IO;

    public sealed class ValueTupleOfFileStreams : IDisposable
    {
        private readonly (FileStream, FileStream) tuple;

        public ValueTupleOfFileStreams(string file1, string file2)
        {
            var stream1 = File.OpenRead(file1);
            var stream2 = File.OpenRead(file2);
            this.tuple = (stream1, stream2);
        }

        public ValueTupleOfFileStreams(string file)
        {
            this.tuple = (File.OpenRead(file), File.OpenRead(file));
        }

        public static void Local(string file)
        {
            var tuple = (File.OpenRead(file), File.OpenRead(file));
            tuple.Item1.Dispose();
            (tuple.Item2 as IDisposable)?.Dispose();
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}
