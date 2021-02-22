// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Collections.Generic;
    using Gu.Reactive;

    public static class UsingGuReactive
    {
        public static ReadOnlyFilteredView<T> M<T>(
            this IObservable<IEnumerable<T>> source,
            Func<T, bool> filter)
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
    }
}
