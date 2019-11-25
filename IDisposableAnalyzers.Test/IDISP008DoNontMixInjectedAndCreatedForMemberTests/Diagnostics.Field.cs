namespace IDisposableAnalyzers.Test.IDISP008DoNontMixInjectedAndCreatedForMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Diagnostics
    {
        public static class Field
        {
            private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

            [TestCase("arg ?? File.OpenRead(string.Empty)")]
            [TestCase("File.OpenRead(string.Empty) ?? arg")]
            [TestCase("true ? arg : File.OpenRead(string.Empty)")]
            [TestCase("true ? File.OpenRead(string.Empty) : arg")]
            [TestCase("Stream ?? File.OpenRead(string.Empty)")]
            [TestCase("File.OpenRead(string.Empty) ?? Stream")]
            [TestCase("true ? Stream : File.OpenRead(string.Empty)")]
            public static void InjectedAndCreated(string expression)
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

        ↓private readonly Stream stream;

        public C(Stream arg)
        {
            this.stream = arg ?? File.OpenRead(string.Empty);
        }
    }
}".AssertReplace("arg ?? File.OpenRead(string.Empty)", expression);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [TestCase("public Stream Stream")]
            [TestCase("internal Stream Stream")]
            public static void MutableFieldInSealed(string property)
            {
                var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓public Stream Stream = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}".AssertReplace("public Stream Stream", property);
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void InjectedAndCreatedCtorAndInitializer()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);

        public C(Stream stream)
        {
            this.stream = stream;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void InjectedAndCreatedTwoCtors()
            {
                var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        ↓private readonly Stream stream;

        public C()
        {
            this.stream = File.OpenRead(string.Empty);
        }

        public C(Stream stream)
        {
            this.stream = stream;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void ProtectedMutable()
            {
                var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓protected Stream stream = File.OpenRead(string.Empty);
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void BackingFieldAssignedWithCreatedAndPropertyWithInjected()
            {
                var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓private Stream stream = File.OpenRead(string.Empty);

        public C(Stream arg)
        {
            this.Stream = arg;
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void BackingFieldAssignedWithInjectedAndPropertyWithCreated()
            {
                var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓private Stream stream;

        public C(Stream arg)
        {
            this.stream = arg;
            this.Stream = File.OpenRead(string.Empty);
        }

        public Stream Stream
        {
            get { return this.stream; }
            private set { this.stream = value; }
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }

            [Test]
            public static void InjectedAndCreatedViaFactory()
            {
                var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        ↓private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public static C Create() => new C(new Disposable());
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, code);
            }

            [Test]
            public static void Issue185()
            {
                var c1 = @"
namespace N
{
    using System;

    public class C1 : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var code = @"
namespace N
{
    using System;

    public class C
    {
        ↓private C1 _c1;

        public C() { }

        public C(C1 c1)
        {
            _c1 = c1;
        }

        public C1 M1() => _c1;

        public C1 M2() => _c1 = new C1();

        public C1 GetOrCreateFoo()
        {
            var c1 = this.M1();
            if (c1 != null)
                return c1;

            c1 = M2();
            if (c1 != null)
                return c1;

            throw new Exception();
        }
    }
}";

                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, c1, code);
            }
        }
    }
}
