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
            Stream disposable;
            if (TryGetStreamCore(out disposable))
            {
                using (disposable)
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
