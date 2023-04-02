namespace ValidCode;

using System.IO;

public class WithWhileLoop
{
    public WithWhileLoop(int i)
    {
        Stream stream = File.OpenRead(string.Empty);
        while (i > 0)
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
            i--;
        }

        stream.Dispose();
    }
}
