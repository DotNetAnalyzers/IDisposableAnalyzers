namespace ValidCode
{
    using System;
    using IDisposableAnnotations;

    public interface IWithAnnotations
    {
        [return: MustDispose]
        IDisposable Create();

        bool TryCreate([MustDispose]out IDisposable disposable);

        [return: DoNotDispose]
        IDisposable GetOrCreate();

        bool TryGet([DoNotDispose]out IDisposable disposable);

        void Add([TransferOwnership] IDisposable disposable);
    }
}
