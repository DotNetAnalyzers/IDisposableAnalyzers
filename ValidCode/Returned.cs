// ReSharper disable All
namespace ValidCode;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Returned
{
    void Consume()
    {
        using var fileOpenReadProperty = this.FileOpenReadProperty;
        using var fileOpenRead = this.FileOpenRead();
        using var yieldReturnFileOpenRead = this.YieldReturnFileOpenRead().Single();
    }

#pragma warning disable IDISP012 // Property should not return created disposable
    IDisposable FileOpenReadProperty => File.OpenRead(string.Empty);
#pragma warning restore IDISP012 // Property should not return created disposable

    IDisposable FileOpenRead() => File.OpenRead(string.Empty);

    IEnumerable<IDisposable> YieldReturnFileOpenRead()
    {
        yield return File.OpenRead(string.Empty);
    }
}
