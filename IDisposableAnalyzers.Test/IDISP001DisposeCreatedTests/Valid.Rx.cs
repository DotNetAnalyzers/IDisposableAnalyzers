namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public static partial class Valid<T>
    {
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
        private readonly IDisposable disposable;

        public C()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
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
        public static void CompositeDisposableAdd()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public C()
        {
            this.disposable.Add(File.OpenRead(string.Empty));
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
        public static void SerialDisposable()
        {
            var code = @"
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
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SerialDisposableObjectInitializer()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly SerialDisposable disposable;

        public C()
        {
            this.disposable = new SerialDisposable { Disposable = File.OpenRead(string.Empty) };
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
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposableCreateClosure()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public sealed class C
    {
        public IObservable<object> Create()
        {
            return Observable.Create<object>(
                o =>
                    {
                        var stream = File.OpenRead(string.Empty);
                        return Disposable.Create(() => stream.Dispose());
                    });
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposableCreateClosureElvis()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public sealed class C
    {
        public IObservable<object> Create()
        {
            return Observable.Create<object>(
                o =>
                    {
                        var stream = File.OpenRead(string.Empty);
                        return Disposable.Create(() => stream?.Dispose());
                    });
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DisposableCreateClosureStatementBody()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    public sealed class C
    {
        public IObservable<object> Create()
        {
            return Observable.Create<object>(
                o =>
                    {
                        var stream = File.OpenRead(string.Empty);
                        return Disposable.Create(() =>
                            {
                                stream.Dispose();
                            });
                    });
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ReturnsCompositeDisposableInitializer()
        {
            var code = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public class C
    {
        internal static IDisposable Create()
        {
            var disposable1 = new Disposable();
            var disposable2 = new Disposable();
            return new CompositeDisposable(2) { disposable1, disposable2 };
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void ReturnsCompositeDisposableLazy()
        {
            var code = @"
namespace N
{
    using System;
    using System.Reactive.Disposables;

    public sealed class C : IDisposable
    {
        private readonly Lazy<IDisposable> disposable;

        public C()
        {
            this.disposable = new Lazy<IDisposable>(() =>
            {
                var temp = new Disposable();
                return new CompositeDisposable(1)
                       {
                           temp
                       };
            });
        }

        public void Dispose()
        {
            if (this.disposable.IsValueCreated)
            {
                this.disposable.Value.Dispose();
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Disposable, code);
        }
    }
}
