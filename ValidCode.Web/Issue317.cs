namespace ValidCode.Web
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class Issue317
    {
        public static async Task M1(string fileName)
        {
            var stream = File.OpenRead(fileName);
            await using var reader1 = new DefaultFalse(stream);
            await using var reader2 = new DefaultFalse(stream, false);
            await using var reader3 = new DefaultTrue(stream, false);
        }

        public static async Task M2(string fileName)
        {
            await using var stream = File.OpenRead(fileName);
            await using var reader1 = new DefaultTrue(stream);
            await using var reader2 = new DefaultTrue(stream, true);
            await using var reader3 = new DefaultFalse(stream, true);
        }

        private sealed class DefaultTrue : IAsyncDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultTrue(Stream stream, bool leaveOpen = true)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public ValueTask DisposeAsync()
            {
                if (!this.leaveOpen)
                {
                    return this.stream.DisposeAsync();
                }

                return ValueTask.CompletedTask;
            }
        }

        private sealed class DefaultFalse : IAsyncDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultFalse(Stream stream, bool leaveOpen = false)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public ValueTask DisposeAsync()
            {
                if (!this.leaveOpen)
                {
                    return this.stream.DisposeAsync();
                }

                return ValueTask.CompletedTask;
            }
        }
    }
}
