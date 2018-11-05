namespace ValidCode
{
    using System;
    using IDisposableAnnotations;

    public interface IWithAnnotations
    {
        [return:MustDispose]
        IDisposable Create();

        bool TryCreate([MustDispose]out IDisposable disposable);

        [return: DonNotDispose]
        IDisposable GetOrCreate();

        bool TryGet([DonNotDispose]out IDisposable disposable);

        void Add([TransferOwnership] IDisposable disposable);
    }
}
