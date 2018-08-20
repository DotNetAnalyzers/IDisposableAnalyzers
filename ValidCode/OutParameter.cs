// ReSharper disable All
namespace ValidCode
{
    using System.IO;

    public class OutParameter
    {
        public static bool TryGetStream(out Stream stream)
        {
            return TryGetStreamCore(out stream);
        }

        public void Baz()
        {
            Stream stream1;
            if (TryGetStreamCore(out stream1))
            {
                using (stream1)
                {
                }
            }

            if (TryGetStreamCore(out var stream2))
            {
                using (stream2)
                {
                }
            }
        }

        private static bool TryGetStreamCore(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}
