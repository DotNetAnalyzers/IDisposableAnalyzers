// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public sealed class Rx : IDisposable
    {
        private readonly IDisposable subscription1;
        private readonly IDisposable subscription2;
        private readonly SingleAssignmentDisposable singleAssignmentDisposable = new SingleAssignmentDisposable();

        public Rx(int no)
            : this(Create(no))
        {
        }

        public Rx(IObservable<object> observable)
        {
            this.subscription1 = observable.Subscribe(_ => { });
            this.subscription2 = observable?.Subscribe(_ => { });
            this.singleAssignmentDisposable.Disposable = observable.Subscribe(_ => { });
        }

        public IDisposable M(bool b) => b ? new Disposable() : System.Reactive.Disposables.Disposable.Empty;

        public void Dispose()
        {
            this.subscription1.Dispose();
            this.subscription2?.Dispose();
            this.singleAssignmentDisposable.Dispose();
        }

        private static IObservable<object> Create(int i)
        {
            return Observable.Empty<object>();
        }
    }
}
