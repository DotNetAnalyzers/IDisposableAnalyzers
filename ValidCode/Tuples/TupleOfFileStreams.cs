namespace ValidCode.Tuples
{
    using System;
    using System.IO;

    public sealed class TupleOfFileStreams : IDisposable
    {
        private readonly Tuple<FileStream, FileStream> tuple;

        public TupleOfFileStreams(string file1, string file2)
        {
            var stream1 = File.OpenRead(file1);
            var stream2 = File.OpenRead(file2);
            this.tuple = Tuple.Create(stream1, stream2);
        }

        public TupleOfFileStreams(string file)
        {
            this.tuple = Tuple.Create(File.OpenRead(file), File.OpenRead(file));
        }

        public TupleOfFileStreams(int file)
        {
            this.tuple = new Tuple<FileStream, FileStream>(File.OpenRead(file.ToString()), File.OpenRead(file.ToString()));
        }

        public void Dispose()
        {
            this.tuple.Item1.Dispose();
            this.tuple.Item2.Dispose();
        }
    }
}
