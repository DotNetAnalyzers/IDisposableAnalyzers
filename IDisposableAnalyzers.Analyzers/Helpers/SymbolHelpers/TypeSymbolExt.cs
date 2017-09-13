namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal static class TypeSymbolExt
    {
        internal static bool TryGetField(this ITypeSymbol type, string name, out IFieldSymbol field)
        {
            return type.TryGetSingleMember(name, out field);
        }

        internal static bool TryGetProperty(this ITypeSymbol type, string name, out IPropertySymbol property)
        {
            return type.TryGetSingleMember(name, out property);
        }

        internal static bool TryGetMethod(this ITypeSymbol type, string name, out IMethodSymbol property)
        {
            return type.TryGetSingleMember(name, out property);
        }

        internal static bool TryGetSingleMember<TMember>(this ITypeSymbol type, string name, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null || string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (var symbol in type.GetMembers(name))
            {
                if (member != null)
                {
                    member = null;
                    return false;
                }

                member = symbol as TMember;
            }

            return member != null;
        }

        internal static bool IsSameType(this ITypeSymbol first, ITypeSymbol other)
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

            return first.Equals(other);
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

            if (conversion.IsNullable && conversion.IsNullLiteral)
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

        internal static bool IsNullable(this ITypeSymbol nullableType, ExpressionSyntax value, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var namedTypeSymbol = nullableType as INamedTypeSymbol;
            if (namedTypeSymbol == null || !namedTypeSymbol.IsGenericType || namedTypeSymbol.Name != "Nullable" || namedTypeSymbol.TypeParameters.Length != 1)
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
            if (type == null)
            {
                return false;
            }

            foreach (var @interface in type.AllInterfaces)
            {
                if (@interface == qualifiedType)
                {
                    return true;
                }
            }

            while (type != null)
            {
                if (type == qualifiedType)
                {
                    return true;
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
    }
}