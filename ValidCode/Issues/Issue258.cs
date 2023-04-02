// ReSharper disable All
namespace ValidCode;

using System;
using System.IO;

public class Issue258 : IDisposable
{
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
            }

            // free unmanaged resources
            if (File.Exists("abc"))
            {
                File.Delete("abc");
            }

            disposedValue = true;
        }
    }

    ~Issue258()
    {
        this.Dispose(false);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
}
