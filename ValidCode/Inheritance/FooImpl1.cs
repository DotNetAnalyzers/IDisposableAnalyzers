namespace ValidCode.Inheritance;

using System.IO;

public class FooImpl1 : FooBase
{
    private readonly Stream stream = File.OpenRead(string.Empty);
    private bool disposed;

    protected override void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        if (disposing)
        {
            this.stream.Dispose();
        }

        base.Dispose(disposing);
    }
}
