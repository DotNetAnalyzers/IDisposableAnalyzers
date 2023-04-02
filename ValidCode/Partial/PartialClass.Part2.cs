namespace ValidCode.Partial;

public partial class PartialClass
{
    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        if (disposing)
        {
            this.disposable.Dispose();
        }
    }
}
