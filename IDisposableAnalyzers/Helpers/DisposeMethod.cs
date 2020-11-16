namespace IDisposableAnalyzers
{
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposeMethod
    {
        internal static IMethodSymbol? Find(ITypeSymbol type, Compilation compilation, Search search)
        {
            if (!type.IsAssignableTo(KnownSymbols.IDisposable, compilation))
            {
                return null;
            }

            if (search == Search.TopLevel)
            {
                if (type.TryFindFirstMethod("Dispose", x => IsMatch(x), out var topLevel))
                {
                    return topLevel;
                }
                else if (type.TryFindFirstMethod("System.IDisposable.Dispose", out topLevel))
                {
                    return topLevel;
                }
                else
                {
                    return null;
                }
            }

            return type.TryFindFirstMethodRecursive("Dispose", x => IsMatch(x), out var recursive)
                ? recursive
                : null;

            static bool IsMatch(IMethodSymbol candidate)
            {
                return candidate is { DeclaredAccessibility: Accessibility.Public, ReturnsVoid: true, Name: "Dispose", Parameters: { Length: 0 } };
            }
        }

        internal static IMethodSymbol? FindVirtual(ITypeSymbol type, Compilation compilation, Search search)
        {
            if (!type.IsAssignableTo(KnownSymbols.IDisposable, compilation))
            {
                return null;
            }

            if (search == Search.TopLevel)
            {
                return type.TryFindFirstMethod("Dispose", x => IsMatch(x), out var topLevel)
                    ? topLevel
                    : null;
            }

            return type.TryFindFirstMethodRecursive("Dispose", x => IsMatch(x), out var recursive)
                ? recursive
                : null;

            static bool IsMatch(IMethodSymbol candidate)
            {
                return IsOverrideDispose(candidate) ||
                       IsVirtualDispose(candidate);
            }
        }

        internal static IMethodSymbol? FindFirst(ITypeSymbol type, Compilation compilation, Search search)
        {
            if (search == Search.TopLevel)
            {
                return Find(type, compilation, Search.TopLevel) ??
                       FindVirtual(type, compilation, Search.TopLevel);
            }

            while (type is { } &&
                   type.IsAssignableTo(KnownSymbols.IDisposable, compilation))
            {
                if (FindFirst(type, compilation, Search.TopLevel) is { } disposeMethod)
                {
                    return disposeMethod;
                }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                type = type.BaseType;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            }

            return null;
        }

        internal static IMethodSymbol? FindDisposeAsync(ITypeSymbol type, Compilation compilation, Search search)
        {
            if (!type.IsAssignableTo(KnownSymbols.IAsyncDisposable, compilation))
            {
                return null;
            }

            if (search == Search.TopLevel)
            {
                return type.TryFindFirstMethod("DisposeAsync", x => IsMatch(x), out var topLevel)
                    ? topLevel
                    : null;
            }

            return type.TryFindFirstMethodRecursive("DisposeAsync", x => IsMatch(x), out var recursive)
                ? recursive
                : null;

            static bool IsMatch(IMethodSymbol candidate)
            {
                return candidate is { DeclaredAccessibility: Accessibility.Public, ReturnsVoid: false, Name: "DisposeAsync", Parameters: { Length: 0 } };
            }
        }

        internal static InvocationExpressionSyntax? FindBaseCall(MethodDeclarationSyntax virtualDispose, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (virtualDispose)
            {
                case { ParameterList: { Parameters: { Count: 0 } } }:
                    {
                        using var walker = InvocationWalker.Borrow(virtualDispose);
                        foreach (var invocation in walker.Invocations)
                        {
                            if (invocation is { Expression: MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax _ } } &&
                                invocation.TryGetMethodName(out var name) &&
                                name == virtualDispose.Identifier.ValueText &&
                                invocation.ArgumentList is { Arguments: { Count: 0 } } &&
                                semanticModel.TryGetSymbol(invocation, cancellationToken, out var target) &&
                                semanticModel.TryGetSymbol(virtualDispose, cancellationToken, out var method) &&
                                method is { IsOverride: true, OverriddenMethod: { } overridden } &&
                                MethodSymbolComparer.Equal(target, overridden))
                            {
                                return invocation;
                            }
                        }

                        break;
                    }

                case { ParameterList: { Parameters: { Count: 1 } parameters } }
                    when parameters.TrySingle(out var parameter):
                    {
                        using var walker = InvocationWalker.Borrow(virtualDispose);
                        foreach (var invocation in walker.Invocations)
                        {
                            if (invocation is { Expression: MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax _ } } &&
                                invocation.TryGetMethodName(out var name) &&
                                name == virtualDispose.Identifier.ValueText &&
                                invocation.ArgumentList is { Arguments: { Count: 1 } arguments } &&
                                arguments[0] is { Expression: IdentifierNameSyntax { Identifier: { ValueText: { } argument } } } &&
                                argument == parameter.Identifier.ValueText &&
                                semanticModel.TryGetSymbol(invocation, cancellationToken, out var target) &&
                                semanticModel.TryGetSymbol(virtualDispose, cancellationToken, out var method) &&
                                method is { IsOverride: true, OverriddenMethod: { } overridden } &&
                                MethodSymbolComparer.Equal(target, overridden))
                            {
                                return invocation;
                            }
                        }

                        break;
                    }
            }

            return null;
        }

        internal static bool IsAccessibleOn(ITypeSymbol type, Compilation compilation)
        {
            if (type.TypeKind == TypeKind.Interface)
            {
                return type.IsAssignableTo(KnownSymbols.IDisposable, compilation);
            }

            return Find(type, compilation, Search.Recursive) is { ExplicitInterfaceImplementations: { IsEmpty: true } };
        }

        internal static bool IsOverrideDispose(IMethodSymbol candidate)
        {
            return candidate is { IsOverride: true, ReturnsVoid: true, Name: "Dispose", Parameters: { Length: 1 } parameters } &&
                   parameters[0].Type.SpecialType == SpecialType.System_Boolean;
        }

        internal static bool IsVirtualDispose(IMethodSymbol candidate)
        {
            return candidate is { IsVirtual: true, ReturnsVoid: true, Name: "Dispose", Parameters: { Length: 1 } parameters } &&
                   parameters[0].Type.SpecialType == SpecialType.System_Boolean;
        }
    }
}
