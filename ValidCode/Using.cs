// ReSharper disable All
namespace ValidCode;

using System;
using System.IO;

public class Using
{
    public Using(IObservable<int> observable)
    {
        using var file = File.OpenRead(string.Empty);

        IDisposable temp;
        using (temp = new Disposable())
        {
        }

        using (new Disposable())
        {
        }

        using (var disposable = new Disposable())
        {
        }

        using (var disposable = File.OpenRead(string.Empty))
        {
        }

        using (File.OpenRead(string.Empty))
        {
        }

        using (var disposable = observable.Subscribe(x => Console.WriteLine(x)))
        {
        }

        using (observable.Subscribe(x => Console.WriteLine(x)))
        {
        }
    }
}
