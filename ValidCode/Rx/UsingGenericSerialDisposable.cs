// ReSharper disable All
namespace ValidCode.Rx;

using System;
using Gu.Reactive;

public sealed class UsingGenericSerialDisposable : IDisposable
{
    private readonly SerialDisposable<IDisposable> serial1 = new();
    private readonly SerialDisposable<IDisposable> serial2 = new();

    public void Update(IObservable<object> observable)
    {
        this.serial1.Disposable = observable.Subscribe(x => this.serial2.Disposable = this.Create());
    }

    private IDisposable Create() => new Disposable();

    public void Dispose()
    {
        this.serial1?.Dispose();
        this.serial2?.Dispose();
    }
}
