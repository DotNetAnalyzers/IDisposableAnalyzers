namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Extension methods that avoids allocations.
    /// </summary>
    internal static partial class EnumerableExt
    {
        /// <summary>
        /// Try getting the element at <paramref name="index"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="index">The index.</param>
        /// <param name="result">The element at index if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryElementAt<T>(this IReadOnlyList<T> source, int index, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            if (index < 0 ||
                source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The single element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle<T>(this IReadOnlyList<T> source, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The single element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle<T>(this IReadOnlyList<T> source, Func<T, bool> predicate, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    for (var j = i + 1; j < source.Count; j++)
                    {
                        if (predicate(source[j]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The first element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst<T>(this IReadOnlyList<T> source, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            if (source.Count == 0)
            {
                return false;
            }

            result = source[0];
            return true;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The first element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst<T>(this IReadOnlyList<T> source, Func<T, bool> predicate, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The last element if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast<T>(this IReadOnlyList<T> source, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            if (source.Count == 0)
            {
                result = default(T);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The last element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast<T>(this IReadOnlyList<T> source, Func<T, bool> predicate, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Try getting the element at <paramref name="index"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="index">The index.</param>
        /// <param name="result">The element at index if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryElementAt<T>(this ImmutableArray<T> source, int index, out T result)
        {
            result = default(T);
            if (index < 0 ||
                source.Length <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The single element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle<T>(this ImmutableArray<T> source, out T result)
        {
            result = default(T);
            if (source.Length == 1)
            {
                result = source[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The single element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle<T>(this ImmutableArray<T> source, Func<T, bool> predicate, out T result)
        {
            result = default(T);
            for (var i = 0; i < source.Length; i++)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    for (var j = i + 1; j < source.Length; j++)
                    {
                        if (predicate(source[j]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The first element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst<T>(this ImmutableArray<T> source, out T result)
        {
            result = default(T);
            if (source.Length == 0)
            {
                return false;
            }

            result = source[0];
            return true;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The first element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst<T>(this ImmutableArray<T> source, Func<T, bool> predicate, out T result)
        {
            result = default(T);
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The last element if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast<T>(this ImmutableArray<T> source, out T result)
        {
            result = default(T);
            if (source.Length == 0)
            {
                result = default(T);
                return false;
            }

            result = source[source.Length - 1];
            return true;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The last element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast<T>(this ImmutableArray<T> source, Func<T, bool> predicate, out T result)
        {
            result = default(T);
            for (var i = source.Length - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Try getting the element at <paramref name="index"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="index">The index.</param>
        /// <param name="result">The element at index if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryElementAt(this ChildSyntaxList source, int index, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (index < 0 ||
                source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The single element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle(this ChildSyntaxList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The single element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle(this ChildSyntaxList source, Func<SyntaxNodeOrToken, bool> predicate, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    for (var j = i + 1; j < source.Count; j++)
                    {
                        if (predicate(source[j]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The first element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst(this ChildSyntaxList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 0)
            {
                return false;
            }

            result = source[0];
            return true;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The first element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst(this ChildSyntaxList source, Func<SyntaxNodeOrToken, bool> predicate, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The last element if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast(this ChildSyntaxList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 0)
            {
                result = default(SyntaxNodeOrToken);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The last element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast(this ChildSyntaxList source, Func<SyntaxNodeOrToken, bool> predicate, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

        /// <summary>
        /// Try getting the element at <paramref name="index"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="index">The index.</param>
        /// <param name="result">The element at index if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryElementAt<T>(this SeparatedSyntaxList<T> source, int index, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (index < 0 ||
                source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The single element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle<T>(this SeparatedSyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The single element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle<T>(this SeparatedSyntaxList<T> source, Func<T, bool> predicate, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    for (var j = i + 1; j < source.Count; j++)
                    {
                        if (predicate(source[j]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The first element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst<T>(this SeparatedSyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 0)
            {
                return false;
            }

            result = source[0];
            return true;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The first element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst<T>(this SeparatedSyntaxList<T> source, Func<T, bool> predicate, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The last element if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast<T>(this SeparatedSyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 0)
            {
                result = default(T);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The last element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast<T>(this SeparatedSyntaxList<T> source, Func<T, bool> predicate, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Try getting the element at <paramref name="index"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="index">The index.</param>
        /// <param name="result">The element at index if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryElementAt<T>(this SyntaxList<T> source, int index, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (index < 0 ||
                source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The single element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle<T>(this SyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The single element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle<T>(this SyntaxList<T> source, Func<T, bool> predicate, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    for (var j = i + 1; j < source.Count; j++)
                    {
                        if (predicate(source[j]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The first element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst<T>(this SyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 0)
            {
                return false;
            }

            result = source[0];
            return true;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The first element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst<T>(this SyntaxList<T> source, Func<T, bool> predicate, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The last element if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast<T>(this SyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 0)
            {
                result = default(T);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in <paramref name="source"/></typeparam>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The last element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast<T>(this SyntaxList<T> source, Func<T, bool> predicate, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Try getting the element at <paramref name="index"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="index">The index.</param>
        /// <param name="result">The element at index if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryElementAt(this SyntaxNodeOrTokenList source, int index, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (index < 0 ||
                source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The single element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle(this SyntaxNodeOrTokenList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The single element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle(this SyntaxNodeOrTokenList source, Func<SyntaxNodeOrToken, bool> predicate, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    for (var j = i + 1; j < source.Count; j++)
                    {
                        if (predicate(source[j]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The first element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst(this SyntaxNodeOrTokenList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 0)
            {
                return false;
            }

            result = source[0];
            return true;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The first element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst(this SyntaxNodeOrTokenList source, Func<SyntaxNodeOrToken, bool> predicate, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The last element if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast(this SyntaxNodeOrTokenList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 0)
            {
                result = default(SyntaxNodeOrToken);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The last element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast(this SyntaxNodeOrTokenList source, Func<SyntaxNodeOrToken, bool> predicate, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

        /// <summary>
        /// Try getting the element at <paramref name="index"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="index">The index.</param>
        /// <param name="result">The element at index if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryElementAt(this SyntaxTokenList source, int index, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            if (index < 0 ||
                source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The single element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle(this SyntaxTokenList source, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The single element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle(this SyntaxTokenList source, Func<SyntaxToken, bool> predicate, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    for (var j = i + 1; j < source.Count; j++)
                    {
                        if (predicate(source[j]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The first element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst(this SyntaxTokenList source, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            if (source.Count == 0)
            {
                return false;
            }

            result = source[0];
            return true;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The first element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst(this SyntaxTokenList source, Func<SyntaxToken, bool> predicate, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The last element if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast(this SyntaxTokenList source, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            if (source.Count == 0)
            {
                result = default(SyntaxToken);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The last element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast(this SyntaxTokenList source, Func<SyntaxToken, bool> predicate, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxToken);
            return false;
        }

        /// <summary>
        /// Try getting the element at <paramref name="index"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="index">The index.</param>
        /// <param name="result">The element at index if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryElementAt(this SyntaxTriviaList source, int index, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            if (index < 0 ||
                source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The single element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle(this SyntaxTriviaList source, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try getting the single element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The single element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TrySingle(this SyntaxTriviaList source, Func<SyntaxTrivia, bool> predicate, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    for (var j = i + 1; j < source.Count; j++)
                    {
                        if (predicate(source[j]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The first element, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst(this SyntaxTriviaList source, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            if (source.Count == 0)
            {
                return false;
            }

            result = source[0];
            return true;
        }

        /// <summary>
        /// Try getting the first element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The first element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryFirst(this SyntaxTriviaList source, Func<SyntaxTrivia, bool> predicate, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="result">The last element if found, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast(this SyntaxTriviaList source, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            if (source.Count == 0)
            {
                result = default(SyntaxTrivia);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        /// <summary>
        /// Try getting the last element in <paramref name="source"/> matching <paramref name="predicate"/>
        /// </summary>
        /// <param name="source">The source collection, can be null.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="result">The last element matching the predicate, can be null.</param>
        /// <returns>True if an element was found.</returns>
        internal static bool TryLast(this SyntaxTriviaList source, Func<SyntaxTrivia, bool> predicate, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxTrivia);
            return false;
        }
    }
}
