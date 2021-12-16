namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    // ReSharper disable once UnusedTypeParameter
    public static partial class Valid<T>
    {
        [TestCase("stream.Dispose()")]
        [TestCase("stream?.Dispose()")]
        [TestCase("((IDisposable)stream)?.Dispose()")]
        [TestCase("(stream as IDisposable)?.Dispose()")]
        public static void DisposedLocal(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public void M()
        {
            var stream = File.OpenRead(String.Empty);
            stream.Dispose();
        }
    }
}".AssertReplace("stream.Dispose()", expression);

            RoslynAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("using (var stream = File.OpenRead(string.Empty))")]
        [TestCase("using (File.OpenRead(string.Empty))")]
        public static void UsedLocal(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }
}".AssertReplace("using (var stream = File.OpenRead(string.Empty))", expression);

            RoslynAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("new C(stream)")]
        [TestCase("new C(stream, false)")]
        public static void LeaveOpen(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C : IDisposable
    {
        private readonly Stream stream;
        private readonly bool leaveOpen;

        public C(Stream stream, bool leaveOpen = false)
        {
            this.stream = stream;
            this.leaveOpen = leaveOpen;
        }

        public static void M(string fileName)
        {
            var stream = File.OpenRead(fileName);
            using var reader = new C(stream);
        }

        public void Dispose()
        {
            if (!this.leaveOpen)
            {
                this.stream.Dispose();
            }
        }
    }
}".AssertReplace("new C(stream)", expression);

            RoslynAssert.Valid(Analyzer, Descriptor, code);
        }
    }
}
