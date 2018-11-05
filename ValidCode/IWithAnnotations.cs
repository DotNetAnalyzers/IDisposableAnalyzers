namespace ValidCode
{
    using System;
    using IDisposableAnnotations;

    public interface IWithAnnotations
    {
        [return:Dispose]
        IDisposable Create();

        [return: DontDispose]
        IDisposable GetOrCreate();

        void Add([Disposes] IDisposable disposable);
    }
}
