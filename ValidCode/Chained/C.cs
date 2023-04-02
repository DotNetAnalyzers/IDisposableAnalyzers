namespace ValidCode.Chained;

using System;

public static class C
{
    public static IDisposable Create() => new Disposable().M();

    public static void Use()
    {
        using var disposable = new Disposable().M();
    }

    public static IDisposable CreateId() => new Disposable().Id();

    public static void UseId()
    {
        using var disposable = new Disposable().Id();
    }
}
