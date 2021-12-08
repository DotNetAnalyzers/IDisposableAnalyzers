// ReSharper disable All
namespace ValidCode.Rx
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public sealed class Misc : IDisposable
    {
        private readonly IDisposable subscription1;
        private readonly IDisposable? subscription2;
        private readonly SingleAssignmentDisposable singleAssignmentDisposable = new SingleAssignmentDisposable();
        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();

        public Misc(int no)
            : this(Create(no))
        {
        }

        public Misc(IObservable<object> observable)
        {
            this.subscription1 = observable.Subscribe(_ => { });
            this.subscription2 = observable?.Subscribe(_ => { });
            this.singleAssignmentDisposable.Disposable = observable?.Subscribe(_ => { });
        }

        public string? Text => this.AddAndReturnToString();
        
        public IDisposable M(bool b) => b ? Disposable.Create(() => { }) : System.Reactive.Disposables.Disposable.Empty;

        internal string? AddAndReturnToString()
        {
            return this.compositeDisposable.AddAndReturn(new ValidCode.Disposable())?.ToString() +
                   this.compositeDisposable.AddAndReturn(File.OpenRead(string.Empty))?.ToString();
        }
        
        public void Dispose()
        {
            this.subscription1.Dispose();
            this.subscription2?.Dispose();
            this.singleAssignmentDisposable.Dispose();
            this.compositeDisposable.Dispose();
        }

        private static IObservable<object> Create(int i)
        {
            return Observable.Empty<object>();
        }

        public static IObservable<EventArgs> ObservableCreateCompositeDisposable()
        {
            return Observable.Create<EventArgs>(
                o =>
                {
                    var tracker = new ValidCode.Disposable();
                    Console.CancelKeyPress += Handler;
                    return new CompositeDisposable(2)
                    {
                        tracker,
                        Disposable.Create(() => Console.CancelKeyPress -= Handler),
                    };

                    void Handler(object? sender, EventArgs e)
                    {
                        o.OnNext(e);
                    }
                });
        }

        public static IObservable<EventArgs> ObservableCreateCompositeDisposable(bool b)
        {
            if (b)
            {
                return Observable.Create<EventArgs>(
                    o =>
                    {
                        var tracker = new ValidCode.Disposable();
                        Console.CancelKeyPress += Handler;
                        return new CompositeDisposable(2)
                        {
                            tracker,
                            Disposable.Create(() => Console.CancelKeyPress -= Handler),
                        };

                        void Handler(object? sender, EventArgs e)
                        {
                            o.OnNext(e);
                        }
                    });
            }

            return Observable.Empty<EventArgs>();
        }
    }
}
