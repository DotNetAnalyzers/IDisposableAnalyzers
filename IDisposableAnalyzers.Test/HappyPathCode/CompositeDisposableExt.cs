// ReSharper disable UnusedMember.Global Used in HappyPathWithAll.PropertyChangedAnalyzersSln
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable
namespace IDisposableAnalyzers.Test.HappyPathCode
{
    using System;
    using System.Reactive.Disposables;

    public static class CompositeDisposableExt
    {
        public static T AddAndReturn<T>(this CompositeDisposable disposable, T item)
            where T : IDisposable
        {
            if (item != null)
            {
                disposable.Add(item);
            }

            return item;
        }
    }
}
