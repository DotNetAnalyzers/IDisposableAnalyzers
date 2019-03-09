namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public partial class ValidCode<T>
    {
        [Test]
        public void AssignLocalWithInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        public C()
        {
            var temp = 1;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssignField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly Disposable disposable;

        public C()
        {
            disposable = new Disposable();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssignFieldLocal()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly Disposable disposable;

        public C()
        {
            var temp = new Disposable();
            this.disposable = temp;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssignProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        public C()
        {
            this.Disposable = new Disposable();
        }

        public Disposable Disposable { get; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssignPropertyLocal()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        public C()
        {
            var temp = new Disposable();
            this.Disposable = temp;
        }

        public Disposable Disposable { get; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssignFieldIndexer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private Disposable[] disposables = new Disposable[2];

        public C()
        {
            for (var i = 0; i < 2; i++)
            {
                var item = new Disposable();
                disposables[i] = item;
            }
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssignFieldListAdd()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public class C
    {
        private List<Disposable> disposables = new List<Disposable>();

        public C()
        {
            for (var i = 0; i < 2; i++)
            {
                var item = new Disposable();
                disposables.Add(item);
            }
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, DisposableCode, testCode);
        }

        [Test]
        public void AssignAssemblyLoadToLocal()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class C
    {
        public void M()
        {
            var assembly = Assembly.Load(string.Empty);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssignedTernary()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        private readonly Stream stream;

        public C()
        {
            var temp = File.OpenRead(string.Empty);
            this.stream = true
                ? temp
                : temp;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssignedCoalesce()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        private readonly Stream stream;

        public C()
        {
            var temp = File.OpenRead(string.Empty);
            this.stream = temp ?? temp;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenDisposedAndReassigned()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public IDisposable M(string fileName)
        {
            var x = File.OpenRead(fileName);
            x.Dispose();
            x = File.OpenRead(fileName);
            return x;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
