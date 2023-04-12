// ReSharper disable All
namespace ValidCode.Tuples;

using System;
using System.IO;

public sealed class DisposingGenericPairOfFileStreams : IDisposable
{
    private readonly Pair<FileStream> pair;

    public DisposingGenericPairOfFileStreams(string file1, string file2)
    {
        var stream1 = File.OpenRead(file1);
        var stream2 = File.OpenRead(file2);
        this.pair = Pair.Create(stream1, stream2);
    }

    public DisposingGenericPairOfFileStreams(string file)
    {
        this.pair = Pair.Create(File.OpenRead(file), File.OpenRead(file));
    }

    public DisposingGenericPairOfFileStreams(int i)
    {
        this.pair = new Pair<FileStream>(File.OpenRead(i.ToString()), File.OpenRead(i.ToString()));
    }

    public static void LocalPairOfFileStreams(string file)
    {
        using (var pair = Pair.Create(File.OpenRead(file), File.OpenRead(file)))
        {
        }
    }

    public void Dispose()
    {
        this.pair.Dispose();
    }

    private static class Pair
    {
#pragma warning disable IDE0090
        public static Pair<T> Create<T>(T item1, T item2) where T : IDisposable => new Pair<T>(item1, item2);
#pragma warning restore IDE0090

        public static Pair<T> CreateImplicit<T>(T item1, T item2) where T : IDisposable => new(item1, item2);
    }

    private sealed class Pair<T> : IDisposable where T : IDisposable
    {
        private readonly T item1;
        private readonly T item2;

        public Pair(T item1, T item2)
        {
            this.item1 = item1;
            this.item2 = item2;
        }

        public void Dispose()
        {
#pragma warning disable IDISP007 // Don't dispose injected
            this.item1.Dispose();
            (this.item2 as IDisposable)?.Dispose();
#pragma warning restore IDISP007
        }
    }
}
