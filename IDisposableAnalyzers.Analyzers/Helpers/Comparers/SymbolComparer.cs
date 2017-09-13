namespace IDisposableAnalyzers
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    internal class SymbolComparer : IEqualityComparer<ISymbol>
    {
        internal static readonly SymbolComparer Default = new SymbolComparer();

        private SymbolComparer()
        {
        }

        public static bool Equals(ISymbol x, ISymbol y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null ||
                y == null)
            {
                return false;
            }

            if (x.Equals(y))
            {
                return true;
            }

            if (x is INamedTypeSymbol xNamed &&
                y is INamedTypeSymbol yNamed &&
                AreEquivalent(xNamed, yNamed))
            {
                return true;
            }

            return x.Equals(y) ||
                   DefinitionEquals(x, y) ||
                   DefinitionEquals(y, x) ||
                   Equals((x as IPropertySymbol)?.OverriddenProperty, y) ||
                   Equals(x, (y as IPropertySymbol)?.OverriddenProperty) ||
                   Equals((x as IMethodSymbol)?.OverriddenMethod, y) ||
                   Equals(x, (y as IMethodSymbol)?.OverriddenMethod);
        }

        bool IEqualityComparer<ISymbol>.Equals(ISymbol x, ISymbol y) => Equals(x, y);

        public int GetHashCode(ISymbol obj)
        {
            return obj?.MetadataName.GetHashCode() ?? 0;
        }

        private static bool AreEquivalent(INamedTypeSymbol first, INamedTypeSymbol other)
        {
            if (ReferenceEquals(first, other))
            {
                return true;
            }

            if (first == null ||
                other == null)
            {
                return false;
            }

            if (first.MetadataName != other.MetadataName ||
                first.ContainingModule.MetadataName != other.ContainingModule.MetadataName ||
                first.Arity != other.Arity)
            {
                return false;
            }

            for (var i = 0; i < first.Arity; i++)
            {
                if (!Equals(first.TypeArguments[i], other.TypeArguments[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool DefinitionEquals(ISymbol x, ISymbol y)
        {
            if (x.IsDefinition && !y.IsDefinition &&
                !ReferenceEquals(y, y.OriginalDefinition))
            {
                return Equals(x, y.OriginalDefinition);
            }

            return false;
        }
    }
}
