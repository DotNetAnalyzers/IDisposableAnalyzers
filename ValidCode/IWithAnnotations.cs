namespace ValidCode
{
    using System;
    using DisposableAnnotations;

    public interface IWithAnnotations
    {
        [return:Dispose]
        IDisposable Create();

        [return: DontDispose]
        IDisposable GetOrCreate();

        void Add([Disposes] IDisposable disposable);
    }
}
