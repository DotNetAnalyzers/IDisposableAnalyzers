// ReSharper disable All
namespace ValidCode;

using System;
using System.Reactive.Disposables;

public sealed class UsingSerialDisposable : IDisposable
{
    private readonly SerialDisposable _serial1 = new SerialDisposable();
    private readonly SerialDisposable _serial2 = new SerialDisposable();

    public void Update(IObservable<object> observable)
    {
        _serial1.Disposable = observable.Subscribe(x => _serial2.Disposable = Create());
    }

    private IDisposable Create() => new Disposable();

    public void Dispose()
    {
        _serial1?.Dispose();
        _serial2?.Dispose();
    }
}
