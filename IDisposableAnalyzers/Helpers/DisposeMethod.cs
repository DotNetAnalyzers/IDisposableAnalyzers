namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposeMethod
    {
        internal static bool TryFindFirst(ITypeSymbol type, Compilation compilation, Search search, out IMethodSymbol disposeMethod)
        {
            if (search == Search.TopLevel)
            {
                return TryFindIDisposableDispose(type, compilation, search, out disposeMethod) ||
                       TryFindVirtualDispose(type, compilation, search, out disposeMethod);
            }

            while (type.IsAssignableTo(KnownSymbol.IDisposable, compilation))
            {
                if (TryFindFirst(type, compilation, Search.TopLevel, out disposeMethod))
                {
                    return true;
                }

                type = type.BaseType;
            }

            disposeMethod = null;
            return false;
        }

        internal static bool TryFindIDisposableDispose(ITypeSymbol type, Compilation compilation, Search search, out IMethodSymbol disposeMethod)
        {
            disposeMethod = null;
            if (!type.IsAssignableTo(KnownSymbol.IDisposable, compilation))
            {
                return false;
            }

            if (search == Search.TopLevel)
            {
                return type.TryFindFirstMethod("Dispose", IsIDisposableDispose, out disposeMethod);
            }

            return type.TryFindFirstMethodRecursive("Dispose", IsIDisposableDispose, out disposeMethod);

            bool IsIDisposableDispose(IMethodSymbol candidate)
            {
                return candidate.Name == "Dispose" &&
                       candidate.ReturnsVoid &&
                       candidate.Parameters.Length == 0 &&
                       candidate.DeclaredAccessibility == Accessibility.Public;
            }
        }

        internal static bool TryFindVirtualDispose(ITypeSymbol type, Compilation compilation, Search search, out IMethodSymbol disposeMethod)
        {
            disposeMethod = null;
            if (!type.IsAssignableTo(KnownSymbol.IDisposable, compilation))
            {
                return false;
            }

            if (search == Search.TopLevel)
            {
                return type.TryFindFirstMethod("Dispose", IsIDisposableDispose, out disposeMethod);
            }

            return type.TryFindFirstMethodRecursive("Dispose", IsIDisposableDispose, out disposeMethod);

            bool IsIDisposableDispose(IMethodSymbol candidate)
            {
                return IsOverrideDispose(candidate) ||
                       IsVirtualDispose(candidate);
            }
        }

        internal static bool TryFindBaseVirtual(ITypeSymbol type, out IMethodSymbol result)
        {
            return type.TryFindFirstMethodRecursive("Dispose", IsVirtualDispose, out result);
        }

        internal static bool TryFindBaseCall(MethodDeclarationSyntax virtualDispose, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax baseCall)
        {
            if (virtualDispose.ParameterList is ParameterListSyntax parameterList &&
                parameterList.Parameters.TrySingle(out var parameter))
            {
                using (var invocations = InvocationWalker.Borrow(virtualDispose))
                {
                    foreach (var invocation in invocations)
                    {
                        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                            memberAccess.Expression is BaseExpressionSyntax &&
                            invocation.TryGetMethodName(out var name) &&
                            name == virtualDispose.Identifier.ValueText &&
                            invocation.ArgumentList is ArgumentListSyntax argumentList &&
                            argumentList.Arguments.TrySingle(out var argument) &&
                            argument.Expression is IdentifierNameSyntax identifierName &&
                            identifierName.Identifier.ValueText == parameter.Identifier.ValueText &&
                            semanticModel.TryGetSymbol(invocation, cancellationToken, out var target) &&
                            semanticModel.TryGetSymbol(virtualDispose, cancellationToken, out var method) &&
                            method.IsOverride &&
                            method.OverriddenMethod is IMethodSymbol overridden &&
                            target.Equals(overridden))
                        {
                            baseCall = invocation;
                            return true;
                        }
                    }
                }
            }

            baseCall = null;
            return false;
        }

        internal static bool IsOverrideDispose(IMethodSymbol candidate)
        {
            return candidate.IsOverride &&
                   candidate.ReturnsVoid &&
                   candidate.Parameters.TrySingle(out var parameter) &&
                   parameter.Type == KnownSymbol.Boolean;
        }

        internal static bool IsVirtualDispose(IMethodSymbol candidate)
        {
            return candidate.IsVirtual &&
                   candidate.ReturnsVoid &&
                   candidate.Parameters.TrySingle(out var parameter) &&
                   parameter.Type == KnownSymbol.Boolean;
        }

        internal static bool IsIDisposableDispose(IMethodSymbol candidate, Compilation compilation)
        {
            return candidate != null &&
                   candidate.Name == "Dispose" &&
                   candidate.ReturnsVoid &&
                   candidate.Parameters.Length == 0 &&
                   candidate.DeclaredAccessibility == Accessibility.Public &&
                   candidate.ContainingType.IsAssignableTo(KnownSymbol.IDisposable, compilation);
        }
    }
}
