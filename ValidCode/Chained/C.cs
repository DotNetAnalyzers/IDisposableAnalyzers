namespace ValidCode.Chained
{
    using System;

    public static class C
    {
        public static IDisposable Create() => new Disposable().M();

        public static void Use()
        {
            using var disposable = new Disposable().M();
        }
    }
}
