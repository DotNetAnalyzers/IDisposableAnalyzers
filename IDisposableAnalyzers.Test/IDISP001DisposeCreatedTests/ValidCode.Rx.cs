namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public partial class ValidCode<T>
    {
        [Test]
        public void CompositeDisposableInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CompositeDisposableAdd()
        {
            var testCode = @"
namespace RoslynSandbox
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SerialDisposable()
        {
            var testCode = @"
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SerialDisposableObjectInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SingleAssignmentDisposable()
        {
            var testCode = @"
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposableCreateClosure()
        {
            var testCode = @"
namespace RoslynSandbox
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposableCreateClosureElvis()
        {
            var testCode = @"
namespace RoslynSandbox
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DisposableCreateClosureStatementBody()
        {
            var testCode = @"
namespace RoslynSandbox
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ReturnsCompositeDisposableInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void ReturnsCompositeDisposableLazy()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, DisposableCode, testCode);
        }
    }
}
