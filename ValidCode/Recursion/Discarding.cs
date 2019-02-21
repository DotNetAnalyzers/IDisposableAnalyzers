namespace ValidCode.Recursion
{
    using System;
    using System.IO;

    public class Discarding
    {
        public Discarding(string fileName)
        {
            M(new Disposable());
            M(File.OpenRead(fileName));
        }

        public static IDisposable M(IDisposable disposable) => M(disposable);
    }
}