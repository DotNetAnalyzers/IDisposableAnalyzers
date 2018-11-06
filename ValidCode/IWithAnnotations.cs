namespace ValidCode
{
    using System;
    using IDisposableAnnotations;

    public interface IWithAnnotations
    {
        [return: GivesOwnership]
        IDisposable Create();

        bool TryCreate([GivesOwnership]out IDisposable disposable);

        [return: KeepsOwnership]
        IDisposable GetOrCreate();

        bool TryGet([KeepsOwnership]out IDisposable disposable);

        void Add([TakesOwnership] IDisposable disposable);
    }
}
