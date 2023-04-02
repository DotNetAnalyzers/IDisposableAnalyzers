namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class Valid
{
    public static class WhenInjecting
    {
        [Test]
        public static void FactoryMethodCallingPrivateCtor()
        {
            var code = @"
namespace N
{
    public class C
    {
        private readonly bool value;

        private C(bool value)
        {
            this.value = value;
        }

        public static C Create() => new C(true);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void FactoryMethodCallingPrivateCtorWithCachedDisposable()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        private static readonly IDisposable Cached = new Disposable();
        private readonly IDisposable value;

        private C(IDisposable value)
        {
            this.value = value;
        }

        public static C Create() => new C(Cached);
    }
}";
            RoslynAssert.Valid(Analyzer, Disposable, code);
        }

        [Test]
        public static void AssignedWithCreatedAndInjected()
        {
            var code = @"
#pragma warning disable IDISP008
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        private readonly IDisposable disposable;

        public C()
        {
            this.disposable = File.OpenRead(string.Empty);
        }

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
