// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class Async
    {
        public async Task Calls()
        {
            var text = await Bar1Async();
            using (await  CreateDisposableAsync())
            {
            }

            using (var disposable = await CreateDisposableAsync())
            {
            }

            using (await Task.FromResult(new Disposable()))
            {
            }
        }

        public static async Task<string> Bar1Async()
        {
            using (var stream = await ReadAsync(string.Empty))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadLine();
                }
            }
        }

        public static async Task<string> Bar2Async()
        {
            using (var stream = await ReadAsync(string.Empty).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadLine();
                }
            }
        }

        private static async Task<Stream> ReadAsync(string fileName)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(fileName))
            {
                await fileStream.CopyToAsync(stream)
                                .ConfigureAwait(false);
            }

            stream.Position = 0;
            return stream;
        }

        private static async Task<IDisposable> CreateDisposableAsync()
        {
            await Task.Delay(0).ConfigureAwait(false);
            return new Disposable();
        }
    }
}
