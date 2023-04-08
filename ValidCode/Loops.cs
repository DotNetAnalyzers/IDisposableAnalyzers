// ReSharper disable All
namespace ValidCode;

using System;
using System.Collections.Generic;
using System.IO;

public sealed class Loops : IDisposable
{
    private readonly List<Disposable> disposables = new();

    public void Update()
    {
        foreach (var disposable in this.disposables)
        {
            disposable.Dispose();
        }

        this.disposables.Clear();
        this.disposables.Add(new Disposable());
    }

    public Stream? DisposeBefore(string[] fileNames)
    {
        Stream? stream = null;
        foreach (var name in fileNames)
        {
            stream?.Dispose();
            stream = File.OpenRead(name);
        }

        return stream;
    }

    public void DisposeAfter(string[] fileNames)
    {
        Stream? stream = null;
        foreach (var name in fileNames)
        {
            stream = File.OpenRead(name);
            stream.Dispose();
        }
    }

    public static void DisposeAfter2()
    {
        for (var i = 0; i < 2; i++)
        {
            IDisposable result;
            result = File.OpenRead(string.Empty);
            result.Dispose();
        }
    }

    public static bool TryGetStreamForEach(string[] fileNames, out Stream? result)
    {
        foreach (var name in fileNames)
        {
            if (name.Length > 5)
            {
                result = File.OpenRead(name);
                return true;
            }
        }

        result = null;
        return false;
    }

    public static bool TryGetStreamFor(string[] fileNames, out Stream? result)
    {
        for (int i = 0; i < fileNames.Length; i++)
        {
            string name = fileNames[i];
            if (name.Length > 5)
            {
                result = File.OpenRead(name);
                return true;
            }
        }

        result = null;
        return false;
    }

    public static bool TryGetStreamWhile(string[] fileNames, out Stream? result)
    {
        var i = 0;
        while (i < fileNames.Length)
        {
            string name = fileNames[i];
            if (name.Length > 5)
            {
                result = File.OpenRead(name);
                return true;
            }

            i++;
        }

        result = null;
        return false;
    }

    public static void While(int i)
    {
        Stream stream = File.OpenRead(string.Empty);
        while (i > 0)
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
            i--;
        }

        stream.Dispose();
    }

    public void Dispose()
    {
        foreach (var disposable in this.disposables)
        {
            disposable.Dispose();
        }
    }
}
