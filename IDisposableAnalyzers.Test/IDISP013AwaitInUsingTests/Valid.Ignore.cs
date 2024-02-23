namespace IDisposableAnalyzers.Test.IDISP013AwaitInUsingTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class Valid
{
    public static class Ignore
    {
        [Test]
        public static void NUnitAssertThrowsAsync()
        {
            var code = """
                namespace N
                {
                    using System;
                    using System.IO;
                    using System.Threading.Tasks;
                    using NUnit.Framework;

                    public class C
                    {
                        public void M()
                        {
                            using (var stream = File.OpenRead(string.Empty))
                            {
                                Assert.ThrowsAsync<Exception>(() => Task.Run(() => throw new Exception()));
                            }
                        }
                    }
                }
                """;
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MoqSetupVerifyAsync()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading;
    using Moq;

    public class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var mock = new Mock<Stream>();
                mock.Setup(x => x.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(0);
                mock.Verify(x => x.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()));
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
