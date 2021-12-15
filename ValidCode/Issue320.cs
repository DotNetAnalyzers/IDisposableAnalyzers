namespace ValidCode;

using System.IO;

public class Issue320
{
    public (MemoryStream Stream, int N) M1()
    {
        var stream = new MemoryStream();
        return (stream, 1);
    }

    public void M2()
    {
        var (stream, n) = this.M1();
        stream.Dispose();
    }
}
