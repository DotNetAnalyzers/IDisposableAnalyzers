namespace IDisposableAnalyzers.Test.IDISP002DisposeMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class Recursion
        {
            [Test]
            public static void IgnoresWhenDisposingRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.RecursiveProperty.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenNotDisposingRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenDisposingFieldAssignedWithRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private IDisposable disposable;

        public C()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenNotDisposingFieldAssignedWithRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private IDisposable disposable;

        public C()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenDisposingRecursiveMethod()
            {
                var code = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable RecursiveMethod() => RecursiveMethod();

        public void Dispose()
        {
            this.RecursiveMethod().Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenDisposingRecursiveMethodChain()
            {
                var code = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable Recursive1() => Recursive2();

        public IDisposable Recursive2() => Recursive1();

        public void Dispose()
        {
            this.Recursive1().Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresRecursiveOutParameter()
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C
    {
        private readonly Stream stream;

        public C()
        {
            if (this.TryGetStream(out this.stream))
            {
            }
        }

        public bool TryGetStream(out Stream outValue)
        {
            outValue = null;
            return this.TryGetStream(out outValue);
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresRecursiveOutParameterChain()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C 
    {
        private readonly Stream stream;

        public C()
        {
            if (this.TryGetStream1(out this.stream))
            {
            }
        }

        public bool TryGetStream1(out Stream outValue1)
        {
            return this.TryGetStream2(out outValue1);
        }

        public bool TryGetStream2(out Stream outValue2)
        {
            return this.TryGetStream1(out outValue2);
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }
        }
    }
}
