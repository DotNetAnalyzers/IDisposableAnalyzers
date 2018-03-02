namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsPotentiallyAssignableTo(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate == null ||
                candidate.IsMissing ||
                candidate is LiteralExpressionSyntax)
            {
                return false;
            }

            if (candidate is ObjectCreationExpressionSyntax objectCreation)
            {
                return IsAssignableTo(semanticModel.GetTypeInfoSafe(objectCreation, cancellationToken).Type);
            }

            return IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(candidate, cancellationToken).Type);
        }

        internal static bool IsPotentiallyAssignableTo(ITypeSymbol type)
        {
            if (type == null ||
                type is IErrorTypeSymbol)
            {
                return false;
            }

            if (type.IsValueType &&
                !IsAssignableTo(type))
            {
                return false;
            }

            if (type.IsSealed &&
                !IsAssignableTo(type))
            {
                return false;
            }

            return true;
        }

        internal static bool IsAssignableTo(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            if (type is ITypeParameterSymbol typeParameter)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    if (IsAssignableTo(constraintType))
                    {
                        return true;
                    }
                }

                return false;
            }

            // https://blogs.msdn.microsoft.com/pfxteam/2012/03/25/do-i-need-to-dispose-of-tasks/
            if (type == KnownSymbol.Task)
            {
                return false;
            }

            return type == KnownSymbol.IDisposable ||
                   type.Is(KnownSymbol.IDisposable);
        }

        internal static bool TryGetDisposeMethod(ITypeSymbol type, Search search, out IMethodSymbol disposeMethod)
        {
            disposeMethod = null;
            if (type == null)
            {
                return false;
            }

            var disposers = type.GetMembers("Dispose");
            switch (disposers.Length)
            {
                case 0:
                    var baseType = type.BaseType;
                    if (search == Search.Recursive &&
                        IsAssignableTo(baseType))
                    {
                        return TryGetDisposeMethod(baseType, Search.Recursive, out disposeMethod);
                    }

                    return false;
                case 1:
                    disposeMethod = disposers[0] as IMethodSymbol;
                    if (disposeMethod == null)
                    {
                        return false;
                    }

                    return (disposeMethod.Parameters.Length == 0 &&
                            disposeMethod.DeclaredAccessibility == Accessibility.Public) ||
                           (disposeMethod.Parameters.Length == 1 &&
                            disposeMethod.Parameters[0].Type == KnownSymbol.Boolean);
                case 2:
                    if (disposers.TrySingle(x => (x as IMethodSymbol)?.Parameters.Length == 1, out ISymbol temp))
                    {
                        disposeMethod = temp as IMethodSymbol;
                        return disposeMethod != null &&
                               disposeMethod.Parameters[0].Type == KnownSymbol.Boolean;
                    }

                    break;
            }

            return false;
        }

        internal static bool TryGetBaseVirtualDisposeMethod(ITypeSymbol type, out IMethodSymbol result)
        {
            bool IsVirtualDispose(IMethodSymbol m)
            {
                return m.IsVirtual &&
                       m.ReturnsVoid &&
                       m.Parameters.Length == 1 &&
                       m.Parameters[0].Type == KnownSymbol.Boolean;
            }

            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.TrySingleMethodRecursive("Dispose", IsVirtualDispose, out result))
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            result = null;
            return false;
        }
    }
}