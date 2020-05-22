namespace ValidCode
{
    using System.IO;

    public class Locals
    {
        public void TempLocal(string file)
        {
            var stream = File.OpenRead(file);
#pragma warning disable IDISP017 // Prefer using.
            var temp = stream;
            temp.Dispose();
#pragma warning restore IDISP017
        }
    }
}
