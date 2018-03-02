namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;

    internal static partial class EnumerableExt
    {
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

            result = default(T);
            return false;
        }

        internal static bool TrySingle<T>(this IReadOnlyList<T> source, Func<T, bool> selector, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        internal static bool TryFirst<T>(this IReadOnlyList<T> source, out T result)
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

            result = source[0];
            return true;
        }

        internal static bool TryFirst<T>(this IReadOnlyList<T> source, Func<T, bool> selector, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

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

        internal static bool TryLast<T>(this IReadOnlyList<T> source, Func<T, bool> selector, out T result)
        {
            result = default(T);
            if (source == null)
            {
                return false;
            }

            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

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

        internal static bool TrySingle<T>(this ImmutableArray<T> source, out T result)
        {
            result = default(T);
            if (source.Length == 1)
            {
                result = source[0];
                return true;
            }

            result = default(T);
            return false;
        }

        internal static bool TrySingle<T>(this ImmutableArray<T> source, Func<T, bool> selector, out T result)
        {
            result = default(T);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        internal static bool TryFirst<T>(this ImmutableArray<T> source, out T result)
        {
            result = default(T);
            if (source.Length == 0)
            {
                result = default(T);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryFirst<T>(this ImmutableArray<T> source, Func<T, bool> selector, out T result)
        {
            result = default(T);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

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

        internal static bool TryLast<T>(this ImmutableArray<T> source, Func<T, bool> selector, out T result)
        {
            result = default(T);
            for (var i = source.Length - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

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

        internal static bool TrySingle(this ChildSyntaxList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

        internal static bool TrySingle(this ChildSyntaxList source, Func<SyntaxNodeOrToken, bool> selector, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

        internal static bool TryFirst(this ChildSyntaxList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 0)
            {
                result = default(SyntaxNodeOrToken);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryFirst(this ChildSyntaxList source, Func<SyntaxNodeOrToken, bool> selector, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

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

        internal static bool TryLast(this ChildSyntaxList source, Func<SyntaxNodeOrToken, bool> selector, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

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

        internal static bool TrySingle<T>(this SeparatedSyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(T);
            return false;
        }

        internal static bool TrySingle<T>(this SeparatedSyntaxList<T> source, Func<T, bool> selector, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        internal static bool TryFirst<T>(this SeparatedSyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 0)
            {
                result = default(T);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryFirst<T>(this SeparatedSyntaxList<T> source, Func<T, bool> selector, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

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

        internal static bool TryLast<T>(this SeparatedSyntaxList<T> source, Func<T, bool> selector, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

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

        internal static bool TrySingle<T>(this SyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(T);
            return false;
        }

        internal static bool TrySingle<T>(this SyntaxList<T> source, Func<T, bool> selector, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        internal static bool TryFirst<T>(this SyntaxList<T> source, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            if (source.Count == 0)
            {
                result = default(T);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryFirst<T>(this SyntaxList<T> source, Func<T, bool> selector, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

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

        internal static bool TryLast<T>(this SyntaxList<T> source, Func<T, bool> selector, out T result)
            where T : SyntaxNode
        {
            result = default(T);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

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

        internal static bool TrySingle(this SyntaxNodeOrTokenList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

        internal static bool TrySingle(this SyntaxNodeOrTokenList source, Func<SyntaxNodeOrToken, bool> selector, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

        internal static bool TryFirst(this SyntaxNodeOrTokenList source, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            if (source.Count == 0)
            {
                result = default(SyntaxNodeOrToken);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryFirst(this SyntaxNodeOrTokenList source, Func<SyntaxNodeOrToken, bool> selector, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

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

        internal static bool TryLast(this SyntaxNodeOrTokenList source, Func<SyntaxNodeOrToken, bool> selector, out SyntaxNodeOrToken result)
        {
            result = default(SyntaxNodeOrToken);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxNodeOrToken);
            return false;
        }

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

        internal static bool TrySingle(this SyntaxTokenList source, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(SyntaxToken);
            return false;
        }

        internal static bool TrySingle(this SyntaxTokenList source, Func<SyntaxToken, bool> selector, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxToken);
            return false;
        }

        internal static bool TryFirst(this SyntaxTokenList source, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            if (source.Count == 0)
            {
                result = default(SyntaxToken);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryFirst(this SyntaxTokenList source, Func<SyntaxToken, bool> selector, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxToken);
            return false;
        }

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

        internal static bool TryLast(this SyntaxTokenList source, Func<SyntaxToken, bool> selector, out SyntaxToken result)
        {
            result = default(SyntaxToken);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxToken);
            return false;
        }

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

        internal static bool TrySingle(this SyntaxTriviaList source, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(SyntaxTrivia);
            return false;
        }

        internal static bool TrySingle(this SyntaxTriviaList source, Func<SyntaxTrivia, bool> selector, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxTrivia);
            return false;
        }

        internal static bool TryFirst(this SyntaxTriviaList source, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            if (source.Count == 0)
            {
                result = default(SyntaxTrivia);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryFirst(this SyntaxTriviaList source, Func<SyntaxTrivia, bool> selector, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(SyntaxTrivia);
            return false;
        }

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

        internal static bool TryLast(this SyntaxTriviaList source, Func<SyntaxTrivia, bool> selector, out SyntaxTrivia result)
        {
            result = default(SyntaxTrivia);
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
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
