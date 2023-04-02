namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests;

using Gu.Roslyn.Asserts;

using NUnit.Framework;

public static partial class Valid<T>
{
    [Test]
    public static void UsingSerialDisposable()
    {
        var notifyPropertyChanged = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        private int p;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int P
        {
            get => this.p;
            set
            {
                if (this.p != value)
                {
                    this.p = value;
                    this.OnPropertyChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
        var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;
    using Gu.Reactive;

    public sealed class C : IDisposable
    {
        private readonly SerialDisposable disposable1 = new SerialDisposable();
        private readonly SerialDisposable disposable2 = new SerialDisposable();

        public void M(NotifyPropertyChanged o)
        {
            this.disposable1.Disposable = o.ObserveValue(x => x.P)
                                           .Subscribe(x => this.disposable2.Disposable = Create());

            IDisposable Create() => new MemoryStream();
        }

        public void Dispose()
        {
            this.disposable1.Dispose();
            this.disposable2.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, new[] { notifyPropertyChanged, code }, settings: LibrarySettings.Reactive);
    }

    [Test]
    public static void UsingGenericSerialDisposable()
    {
        var notifyPropertyChanged = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        private int p;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int P
        {
            get => this.p;
            set
            {
                if (this.p != value)
                {
                    this.p = value;
                    this.OnPropertyChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
        var code = @"
namespace N
{
    using System;
    using System.IO;
    using Gu.Reactive;

    public sealed class C : IDisposable
    {
        private readonly SerialDisposable<IDisposable> disposable1 = new SerialDisposable<IDisposable>();
        private readonly SerialDisposable<IDisposable> disposable2 = new SerialDisposable<IDisposable>();

        public void M(NotifyPropertyChanged o)
        {
            this.disposable1.Disposable = o.ObserveValue(x => x.P)
                                           .Subscribe(x => this.disposable2.Disposable = Create());

            IDisposable Create() => new MemoryStream();
        }

        public void Dispose()
        {
            this.disposable1.Dispose();
            this.disposable2.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, new[] { notifyPropertyChanged, code }, settings: LibrarySettings.Reactive);
    }

    [Test]
    public static void AssigningGenericSerialDisposable()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    using Gu.Reactive;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;
        private readonly SerialDisposable<MemoryStream> serialDisposable = new SerialDisposable<MemoryStream>();

        public C(IObservable<int> observable)
        {
            this.disposable = observable.Subscribe(x => this.serialDisposable.Disposable = new MemoryStream());
        }

        public void Dispose()
        {
            this.serialDisposable.Dispose();
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code, settings: LibrarySettings.Reactive);
    }
}
