// ReSharper disable All
namespace ValidCode.Rx
{
    using System;
    using System.Reactive.Disposables;
    using Gu.Reactive;
    using Gu.Wpf.Reactive;

    public sealed class Issue252 : IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public Issue252(ICondition c1, ICondition c2)
        {
            AbortCommand = new ConditionRelayCommand(
                () => { },
                _disposable.AddAndReturn(new OrCondition(c1, c2))!);
        }

        public ConditionRelayCommand AbortCommand { get; }

        public void Dispose()
        {
            _disposable.Dispose();
            AbortCommand.Dispose();
        }
    }
}
