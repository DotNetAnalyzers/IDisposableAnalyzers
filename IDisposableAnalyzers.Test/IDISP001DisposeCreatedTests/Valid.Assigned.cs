namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public static partial class Valid<T>
    {
        [Test]
        public static void AssignLocalWithInt()
        {
            var code = @"
#pragma warning disable CS0219
namespace N
{
    public class C
    {
        public C()
        {
            var temp = 1;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void AssignField()
        {
            var code = @"
namespace N
{
    public class C
    {
        private readonly Disposable _disposable;

        public C()
        {
            _disposable = new Disposable();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void AssignFieldLocal()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void AssignFieldViaLocal()
        {
            var code = @"
namespace N
{
    public class C
    {
        private readonly Disposable disposable;

        public C()
        {
            var temp = new Disposable();
            var temp2 = temp;
            this.disposable = temp2;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void AssignFieldViaParameter()
        {
            var code = @"
namespace N
{
    public class C
    {
        private Disposable? disposable;

        public C()
        {
            var temp = new Disposable();
            M(temp);
        }

        private void M(Disposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, Descriptor, Disposable, code);
        }

        [Test]
        public static void AssignProperty()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void AssignPropertyLocal()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void AssignFieldIndexer()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void AssignFieldListAdd()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void AssignAssemblyLoadToLocal()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AssignedTernary()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AssignedCoalesce()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private readonly Stream stream;

        public C()
        {
            var temp = File.OpenRead(string.Empty);
            this.stream = temp ?? temp ?? throw new Exception();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenDisposedAndReassigned()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
