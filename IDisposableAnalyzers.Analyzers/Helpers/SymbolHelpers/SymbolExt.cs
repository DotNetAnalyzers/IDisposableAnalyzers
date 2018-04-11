// ReSharper disable UnusedMember.Global
namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static partial class SymbolExt
    {
        internal static bool IsEither<T1, T2>(this ISymbol symbol)
            where T1 : ISymbol
            where T2 : ISymbol
        {
            return symbol is T1 || symbol is T2;
        }
    }
}
