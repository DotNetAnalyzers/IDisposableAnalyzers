namespace ValidCode
{
    using System;
    using System.IO;

    public class Issue134
    {
        public IDisposable M2(string fileName)
        {
            var x = File.OpenRead(fileName);
            x.Dispose();
            x = File.OpenRead(fileName);
            return x;
        }
    }
}
