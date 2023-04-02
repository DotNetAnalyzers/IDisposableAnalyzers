namespace ValidCode.Rx;

using System;
using System.Reactive.Disposables;

public sealed class Issue298 : IDisposable
{
    private readonly CompositeDisposable disposable = new();

    public Issue298(IObservable<object> observable)
    {
        observable.Subscribe(x => Console.WriteLine(x))
                  .DisposeWith(this.disposable);
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}
