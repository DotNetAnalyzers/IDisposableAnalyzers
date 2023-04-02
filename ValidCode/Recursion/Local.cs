namespace ValidCode.Recursion;

using System;

public class Local
{
    public Local(IDisposable disposable)
    {
        var value = disposable;
#pragma warning disable CS1717
        value = value;
    }
}
