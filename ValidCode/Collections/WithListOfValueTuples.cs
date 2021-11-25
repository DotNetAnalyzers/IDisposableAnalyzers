// ReSharper disable All
namespace ValidCode.Collections
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public sealed class WithListOfValueTuples : IDisposable
    {
        private readonly List<(FileStream, FileStream)> xs = new List<(FileStream, FileStream)>();

        public void M(string file1, string file2)
        {
            var tuple = (File.OpenRead(file1), File.OpenRead(file2));
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
