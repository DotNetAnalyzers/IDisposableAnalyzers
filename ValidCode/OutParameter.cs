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
            else
            {
                stream1?.Dispose();
            }

            if (TryGetStreamCore(out var stream2))
            {
                using (stream2)
                {
                }
            }
            else
            {
                stream2?.Dispose();
            }
        }

        public void ExpressionBody(out IDisposable disposable) => disposable = File.OpenRead(string.Empty);

        private static bool TryGetStreamCore(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }

        private static bool TryGetStream(string fileName, out Stream? result)
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

        public void CallTryId(string fileName)
        {
            if (TryId(File.OpenRead(fileName), out var stream))
            {
                using (stream)
                {
                }
            }
            else
            {
                stream?.Dispose();
            }
        }

        public static bool TryId<T>(T item, out T result)
        {
            result = item;
            return true;
        }

        public static void ReassignParameter(IDisposable disposable)
        {
            if (TryReassign(disposable, out disposable))
            {
                using (disposable)
                {
                }
            }
            else
            {
                disposable?.Dispose();
            }
        }

        private static bool TryReassign(IDisposable old, out IDisposable result)
        {
            result = new Disposable();
            return true;
        }
    }
}
