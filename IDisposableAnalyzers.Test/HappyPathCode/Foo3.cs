// ReSharper disable UnusedMember.Global Used in HappyPathWithAll.PropertyChangedAnalyzersSln
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
namespace IDisposableAnalyzers.Test.HappyPathCode
{
    using System;
    using System.IO;

    public class Foo3
    {
        public static bool TryGetStream(out Stream stream)
        {
            return TryGetStreamCore(out stream);
        }

        public void Bar()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
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
