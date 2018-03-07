namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        internal static bool IsRepresentationPreservingConversion(
            this ITypeSymbol toType,
            ExpressionSyntax valueExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var conversion = semanticModel.SemanticModelFor(valueExpression)
                                          .ClassifyConversion(valueExpression, toType);
            if (!conversion.Exists)
            {
                return false;
            }

            if (conversion.IsIdentity)
            {
                return true;
            }

            if (conversion.IsReference &&
                conversion.IsImplicit)
            {
                return true;
            }

            if (conversion.IsNullable &&
                conversion.IsNullLiteral)
            {
                return true;
            }

            if (conversion.IsBoxing ||
                conversion.IsUnboxing)
            {
                return true;
            }

            if (toType.IsNullable(valueExpression, semanticModel, cancellationToken))
            {
                return true;
            }

            return false;
        }

        internal static bool IsNullable(
            this ITypeSymbol nullableType,
            ExpressionSyntax value,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var namedTypeSymbol = nullableType as INamedTypeSymbol;
            if (namedTypeSymbol == null ||
                !namedTypeSymbol.IsGenericType ||
                namedTypeSymbol.Name != "Nullable" ||
                namedTypeSymbol.TypeParameters.Length != 1)
            {
                return false;
            }

            if (value.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return true;
            }

            var typeInfo = semanticModel.GetTypeInfoSafe(value, cancellationToken);
            return namedTypeSymbol.TypeArguments[0].IsSameType(typeInfo.Type);
        }

        internal static bool Is(this ITypeSymbol type, QualifiedType qualifiedType)
        {
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
            if (other.IsInterface())
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

        internal static bool IsInterface(this ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            return type != KnownSymbol.Object && type.BaseType == null;
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
