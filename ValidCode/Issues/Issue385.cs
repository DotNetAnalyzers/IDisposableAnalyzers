namespace ValidCode;

using System;
using System.Collections.ObjectModel;

using Gu.Reactive;

public sealed class Issue385 : IDisposable
{
    private readonly System.Reactive.Disposables.CompositeDisposable disposable;
    private bool disposed;

    private Issue385()
    {
        this.disposable = new System.Reactive.Disposables.CompositeDisposable
        {
            this.Xs.ObserveCollectionChangedSlim(signalInitial: false)
                   .Subscribe(_ => {}),
        };
    }

    public ObservableCollection<int> Xs { get; } = new();

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.disposable?.Dispose();
    }
}
