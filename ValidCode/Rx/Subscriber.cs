// ReSharper disable All
namespace ValidCode.Rx;

using System;
using System.Reactive.Linq;
using Gu.Reactive;

public sealed class Subscriber : IDisposable
{
    private readonly IDisposable disposable;
    
    public Subscriber(NotifyPropertyChanged o)
    {
        this.disposable = o.ObserveValue(x => x.Value)
                           .Where(x => x.Value > 2)
                           .Subscribe(x => Console.WriteLine(x));
    }

    public void Dispose()
    {
        this.disposable?.Dispose();
    }
}
