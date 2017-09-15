namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        public class Assigned
        {
            [Test]
            public void AssignField()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Disposable disposable;

        public Foo()
        {
            disposable = new Disposable();
        }
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(DisposableCode, testCode);
            }

            [Test]
            public void AssignFieldLocal()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Disposable disposable;

        public Foo()
        {
            var temp = new Disposable();
            this.disposable = temp;
        }
    }
}";

                AnalyzerAssert.Valid<IDISP001DisposeCreated>(DisposableCode, testCode);
            }

            [Test]
            public void AssignProperty()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            this.Disposable = new Disposable();
        }

        public Disposable Disposable { get; }
    }
}";

                AnalyzerAssert.Valid<IDISP001DisposeCreated>(DisposableCode, testCode);
            }

            [Test]
            public void AssignPropertyLocal()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Disposable disposable;

        public Foo()
        {
            var temp = new Disposable();
            this.Disposable = temp;
        }

        public Disposable Disposable { get; }
    }
}";

                AnalyzerAssert.Valid<IDISP001DisposeCreated>(DisposableCode, testCode);
            }

            [Test]
            public void AssignFieldIndexer()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";

                AnalyzerAssert.Valid<IDISP001DisposeCreated>(DisposableCode, testCode);
            }

            [Test]
            public void AssignFieldListAdd()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";

                AnalyzerAssert.Valid<IDISP001DisposeCreated>(DisposableCode, testCode);
            }

            [Test]
            public void BuildCollectionThenAssignField()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";

                AnalyzerAssert.Valid<IDISP001DisposeCreated>(DisposableCode, testCode);
            }

            [Test]
            public void AssignAssemblyLoadToLocal()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class Foo
    {
        public void Bar()
        {
            var assembly = Assembly.Load(string.Empty);
        }
    }
}";
                AnalyzerAssert.Valid<IDISP001DisposeCreated>(testCode);
            }
        }
    }
}