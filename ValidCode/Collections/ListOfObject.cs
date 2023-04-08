// ReSharper disable All
namespace ValidCode.Collections;

using System;
using System.Collections.Generic;

internal sealed class ListOfObject : IDisposable
{
    private readonly List<object> disposables = new();

    public ListOfObject()
    {
        this.disposables.Add(new Disposable());
    }

    public void Dispose()
    {
        foreach (var disposable in this.disposables)
        {
            (disposable as IDisposable)?.Dispose();
        }
    }
}
