namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<IDISP001DisposeCreated>
    {
        public class Assigned : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task AssignField()
            {
                var testCode = @"
public class Foo
{
    private readonly Disposable disposable;

    public Foo()
    {
        disposable = new Disposable();
    }
}";

                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task AssignFieldLocal()
            {
                var testCode = @"
public class Foo
{
    private readonly Disposable disposable;

    public Foo()
    {
        var temp = new Disposable();
        this.disposable = temp;
    }
}";

                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task AssignProperty()
            {
                var testCode = @"
public class Foo
{
    public Foo()
    {
        this.Disposable = new Disposable();
    }

    public Disposable Disposable { get; }
}";

                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task AssignPropertyLocal()
            {
                var testCode = @"
public class Foo
{
    private readonly Disposable disposable;

    public Foo()
    {
        var temp = new Disposable();
        this.Disposable = temp;
    }

    public Disposable Disposable { get; }
}";

                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task AssignFieldIndexer()
            {
                var testCode = @"
public class Foo
{
    private Disposable[] disposables = new Disposable[2];

    public Foo()
    {
        for (var i = 0; i < 2; i++)
        {
            var item = new Disposable();
            disposables[i] = item;
        }
    }
}";

                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task AssignFieldListAdd()
            {
                var testCode = @"
using System.Collections.Generic;

public class Foo
{
    private List<Disposable> disposables = new List<Disposable>();

    public Foo()
    {
        for (var i = 0; i < 2; i++)
        {
            var item = new Disposable();
            disposables.Add(item);
        }
    }
}";

                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task BuildCollectionThenAssignField()
            {
                var testCode = @"
public class Foo
{
    private Disposable[] disposables;

    public Foo()
    {
        var items = new Disposable[2];
        for (var i = 0; i < 2; i++)
        {
            var item = new Disposable();
            items[i] = item;
        }

        this.disposables = items;
    }
}";

                await this.VerifyHappyPathAsync(DisposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task AssignAssemblyLoadToLocal()
            {
                var testCode = @"
using System.Reflection;

public class Foo
{
    public void Bar()
    {
        var assembly = Assembly.Load(string.Empty);
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                            .ConfigureAwait(false);
            }
        }
    }
}