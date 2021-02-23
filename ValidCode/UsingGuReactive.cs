// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Collections.Generic;

    using Gu.Reactive;

    public sealed class UsingGuReactive : IDisposable
    {
        private readonly IReadOnlyView<int> view;

        public UsingGuReactive(IObservable<IEnumerable<int>> source, Func<int, bool> filter)
        {
            this.view = source.AsReadOnlyView()
                              .AsReadOnlyFilteredView(filter, leaveOpen: false);
        }

        public static ReadOnlyFilteredView<T> M1<T>(IObservable<IEnumerable<T>> source, Func<T, bool> filter)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return source.AsReadOnlyView()
                         .AsReadOnlyFilteredView(filter, leaveOpen: false);
        }

        public static IReadOnlyView<IDisposable> M2(IObservable<IEnumerable<int>> source, Func<int, bool> filter)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return source.AsReadOnlyFilteredView(filter).AsMappingView(x => new Disposable(), onRemove:x => x.Dispose());
        }

        public static void M2<T>(IObservable<IEnumerable<T>> source, Func<T, bool> filter)
        {
            using var view = source.AsReadOnlyView().AsReadOnlyFilteredView(filter, leaveOpen: false);
        }

        public void Dispose()
        {
            this.view.Dispose();
        }
    }
}
