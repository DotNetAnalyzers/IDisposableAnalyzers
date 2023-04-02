// ReSharper disable All
namespace ValidCode.Collections;

using System.Collections.Generic;
using System.IO;

public class DictionaryOfIntAndStream
{
    private readonly Dictionary<int, Stream> streams = new();

    public DictionaryOfIntAndStream()
    {
        this.streams[0] = File.OpenRead(string.Empty);
    }

    public Stream Get(int i)
    {
        return this.streams[i];
    }

    public void Set(int i, string fileName)
    {
        if (this.streams.TryGetValue(i, out var stream))
        {
            stream.Dispose();
        }

        this.streams[i] = File.OpenRead(fileName);
    }
}
