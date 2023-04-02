namespace ValidCode.Tuples;

using System;
using System.IO;

public sealed class PairOfFileStreams : IDisposable
{
    private readonly Pair<FileStream> pair;

    public PairOfFileStreams(string file1, string file2)
    {
        var stream1 = File.OpenRead(file1);
        var stream2 = File.OpenRead(file2);
        this.pair = Pair.Create(stream1, stream2);
    }

    public PairOfFileStreams(string file)
    {
        this.pair = Pair.Create(File.OpenRead(file), File.OpenRead(file));
    }

    public PairOfFileStreams(int i)
    {
        this.pair = new Pair<FileStream>(File.OpenRead(i.ToString()), File.OpenRead(i.ToString()));
    }

    public static void LocalPairOfFileStreams(string file)
    {
        var pair = Pair.Create(File.OpenRead(file), File.OpenRead(file));
        pair.Item1.Dispose();
        (pair.Item2 as IDisposable)?.Dispose();
    }

    public void Dispose()
    {
        this.pair.Item1.Dispose();
        this.pair.Item2.Dispose();
    }

    public static class Pair
    {
        public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
    }

    public class Pair<T>
    {
        public Pair(T item1, T item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; }

        public T Item2 { get; }
    }
}
