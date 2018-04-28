namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal static partial class TypeSymbolExt
    {
        internal static bool IsSameType(this ITypeSymbol first, ITypeSymbol other)
        {
            if (ReferenceEquals(first, other) ||
                first?.Equals(other) == true)
            {
                return true;
            }

            if (first is ITypeParameterSymbol firstParameter &&
                other is ITypeParameterSymbol otherParameter)
            {
                return firstParameter.MetadataName == otherParameter.MetadataName &&
                       firstParameter.ContainingSymbol.Equals(otherParameter.ContainingSymbol);
            }

            return first is INamedTypeSymbol firstNamed &&
                   other is INamedTypeSymbol otherNamed &&
                   IsSameType(firstNamed, otherNamed);
        }

        internal static bool IsSameType(this INamedTypeSymbol first, INamedTypeSymbol other)
        {
            if (first == null ||
                other == null)
            {
                return false;
            }

            if (first.IsDefinition ^ other.IsDefinition)
            {
                return IsSameType(first.OriginalDefinition, other.OriginalDefinition);
            }

            return first.Equals(other) ||
                   AreEquivalent(first, other);
        }

        internal static bool Is(this ITypeSymbol type, QualifiedType qualifiedType)
        {
            if (type is ITypeParameterSymbol typeParameter)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    if (Is(constraintType, qualifiedType))
                    {
                        return true;
                    }
                }

                return false;
            }

            while (type != null)
            {
                if (type == qualifiedType)
                {
                    return true;
                }

                foreach (var @interface in type.AllInterfaces)
                {
                    if (@interface == qualifiedType)
                    {
                        return true;
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        internal static bool Is(this ITypeSymbol type, ITypeSymbol other)
        {
            if (other.TypeKind == TypeKind.Interface)
            {
                foreach (var @interface in type.AllInterfaces)
                {
                    if (IsSameType(@interface, other))
                    {
                        return true;
                    }
                }

                return false;
            }

            while (type?.BaseType != null)
            {
                if (IsSameType(type, other))
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        internal static bool AreEquivalent(this INamedTypeSymbol first, INamedTypeSymbol other)
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
                if (!IsSameType(first.TypeArguments[i], other.TypeArguments[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
