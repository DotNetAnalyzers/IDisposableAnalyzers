namespace ValidCode.Tuples
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public sealed class ListOfTupleOfFileStreams : IDisposable
    {
        private readonly List<Tuple<FileStream, FileStream>> xs = new List<Tuple<FileStream, FileStream>>();

        public void M(string file1, string file2)
        {
            var tuple = Tuple.Create(File.OpenRead(file1), File.OpenRead(file2));
            this.xs.Add(tuple);
        }

        public void M(string file)
        {
            var stream1 = File.OpenRead(file);
            var stream2 = File.OpenRead(file);
            var tuple = Tuple.Create(stream1, stream2);
            this.xs.Add(tuple);
        }

        public void Dispose()
        {
            foreach (var tuple in this.xs)
            {
                tuple.Item1.Dispose();
                tuple.Item2.Dispose();
            }
        }
    }
}
