// ReSharper disable UnusedMember.Global Used in HappyPathWithAll.PropertyChangedAnalyzersSln
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
namespace IDisposableAnalyzers.Test.HappyPathCode
{
    using System;
    using System.Collections.Generic;

    public class Foo2
    {
        private IDisposable disposable;

        public Foo2(IDisposable disposable)
        {
            this.disposable = Bar(disposable);
        }

        private static IDisposable Bar(IDisposable disposable, IEnumerable<IDisposable> disposables = null)
        {
            if (disposables == null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }
    }
}
