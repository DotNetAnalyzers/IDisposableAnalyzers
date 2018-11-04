// ReSharper disable All
namespace ValidCode
{
    using System;
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

        public void ExpressionBody(out IDisposable disposable) => disposable = File.OpenRead(string.Empty);

        private static bool TryGetStreamCore(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }

        private static bool TryGetStream(string fileName, out Stream result)
        {
            if (File.Exists(fileName))
            {
                result = File.OpenRead(fileName);
                return true;
            }

            if (File.Exists(fileName))
            {
                result = File.OpenRead(fileName);
                return true;
            }

            result = null;
            return false;
        }
    }
}
