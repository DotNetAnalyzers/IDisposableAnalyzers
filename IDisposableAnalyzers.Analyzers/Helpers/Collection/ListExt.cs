namespace IDisposableAnalyzers
{
    using System.Collections.Generic;

    internal static class ListExt
    {
        internal static void PurgeDuplicates<T>(this List<T> list, IEqualityComparer<T> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<T>.Default;
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = list.Count - 1; j > i; j--)
                {
                    if (comparer.Equals(list[i], list[j]))
                    {
                        list.RemoveAt(j);
                    }
                }
            }
        }
    }
}