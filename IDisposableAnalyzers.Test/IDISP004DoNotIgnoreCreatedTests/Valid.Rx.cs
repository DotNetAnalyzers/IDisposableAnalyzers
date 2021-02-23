namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        [Test]
        public static void SerialDisposable()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly SerialDisposable disposable = new SerialDisposable();

        public void Update()
        {
            this.disposable.Disposable = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SingleAssignmentDisposable()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly SingleAssignmentDisposable disposable = new SingleAssignmentDisposable();

        public void Update()
        {
            this.disposable.Disposable = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CompositeDisposableInitializer()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable;

        public C()
        {
            this.disposable = new CompositeDisposable
            {
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty),
            };
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CompositeDisposableCtor()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable;

        public C()
        {
            this.disposable = new CompositeDisposable(
                File.OpenRead(string.Empty),
                File.OpenRead(string.Empty));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CompositeDisposableAddIObservableSubscribe()
        {
            var code = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public C(IObservable<object> observable)
        {
            this.disposable.Add(observable.Subscribe(_ => { }));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CompositeDisposableAddNewSingleAssignmentDisposable()
        {
            var code = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public C()
        {
            this.disposable.Add(new SingleAssignmentDisposable());
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CompositeDisposableAddThrottleSubscribe()
        {
            var code = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public C(IObservable<object> observable)
        {
            this.disposable.Add(observable.Throttle(TimeSpan.FromMilliseconds(100))
                                          .Subscribe(_ => { }));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CompositeDisposableExtAddAndReturn()
        {
            var compositeDisposableExtCode = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public static class CompositeDisposableExt
    {
        public static T AddAndReturn<T>(this CompositeDisposable disposable, T item)
            where T : IDisposable
        {
            if (item != null)
            {
                disposable.Add(item);
            }

            return item;
        }
    }
}";
            var code = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public void Dispose()
        {
            this.disposable.Dispose();
        }

        internal object AddAndReturn()
        {
            return disposable.AddAndReturn(new Disposable());
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, compositeDisposableExtCode, code);
        }

        [Test]
        public static void CompositeDisposableExtAddAndReturnToString()
        {
            var compositeDisposableExtCode = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public static class CompositeDisposableExt
    {
        public static T AddAndReturn<T>(this CompositeDisposable disposable, T item)
            where T : IDisposable
        {
            if (item != null)
            {
                disposable.Add(item);
            }

            return item;
        }
    }
}";
            var code = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public void Dispose()
        {
            this.disposable.Dispose();
        }

        internal string AddAndReturnToString()
        {
            return disposable.AddAndReturn(new Disposable()).ToString();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, DisposableCode, compositeDisposableExtCode, code);
        }

        [Test]
        public static void ISchedulerSchedule()
        {
            var code = @"
namespace N
{
    using System;
    using System.Reactive.Concurrency;

    class C
    {
        internal void M(IScheduler scheduler)
        {
            scheduler.Schedule(() => Console.WriteLine());
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ObservableElvisSubscribeIssue221()
        {
            var code = @"
namespace N
{
    using System;
    using System.Reactive.Linq;

    sealed class C : IDisposable
    {
        private readonly IDisposable subscription;

        private bool disposed;

        public C(IObservable<object> observable = null)
        {
            this.subscription = observable?.Subscribe(_ => { });
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.subscription?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AsReadOnlyViewAsReadOnlyFilteredView()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using Gu.Reactive;

    public static class C
    {
        public static ReadOnlyFilteredView<T> M<T>(
            this IObservable<IEnumerable<T>> source,
            Func<T, bool> filter)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return source.AsReadOnlyView()
                         .AsReadOnlyFilteredView(filter, leaveOpen: false);
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AsReadOnlyFilteredViewAsMappingView()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Gu.Reactive;

    public static class C
    {
        public static IReadOnlyView<IDisposable> M2(IObservable<IEnumerable<int>> source, Func<int, bool> filter)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return source.AsReadOnlyFilteredView(filter).AsMappingView(x => new MemoryStream(), onRemove:x => x.Dispose());
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
