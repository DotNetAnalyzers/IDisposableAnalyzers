namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposeMethod
    {
        internal static IMethodSymbol? Find(ITypeSymbol type, Compilation compilation, Search search)
        {
            if (!type.IsAssignableTo(KnownSymbol.IDisposable, compilation))
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
                return candidate is { DeclaredAccessibility: Accessibility.Public, ReturnsVoid: true, Name: "Dispose", Parameters: { Length: 0 } };
            }
        }

        internal static IMethodSymbol? FindVirtual(ITypeSymbol type, Compilation compilation, Search search)
        {
            if (!type.IsAssignableTo(KnownSymbol.IDisposable, compilation))
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

            while (type.IsAssignableTo(KnownSymbol.IDisposable, compilation))
            {
                if (FindFirst(type, compilation, Search.TopLevel) is { } disposeMethod)
                {
                    return disposeMethod;
                }

                type = type.BaseType;
            }

            return null;
        }

        internal static bool IsAccessibleOn(ITypeSymbol type, Compilation compilation)
        {
            if (type.TypeKind == TypeKind.Interface)
            {
                return type.IsAssignableTo(KnownSymbol.IDisposable, compilation);
            }

            return Find(type, compilation, Search.Recursive) is { ExplicitInterfaceImplementations: { IsEmpty: true } };
        }

        internal static bool TryFindBaseCall(MethodDeclarationSyntax virtualDispose, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out InvocationExpressionSyntax? baseCall)
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
                                target.Equals(overridden))
                            {
                                baseCall = invocation;
                                return true;
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
                                target.Equals(overridden))
                            {
                                baseCall = invocation;
                                return true;
                            }
                        }

                        break;
                    }
            }

            baseCall = null;
            return false;
        }

        internal static bool TryFindSuppressFinalizeCall(MethodDeclarationSyntax disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out InvocationExpressionSyntax? suppressCall)
        {
            using (var walker = InvocationWalker.Borrow(disposeMethod))
            {
                foreach (var candidate in walker.Invocations)
                {
                    if (candidate.ArgumentList is { Arguments: { Count: 1 } } &&
                        candidate.TryGetMethodName(out var name) &&
                        name == "SuppressFinalize" &&
                        semanticModel.TryGetSymbol(candidate, KnownSymbol.GC.SuppressFinalize, cancellationToken, out _))
                    {
                        suppressCall = candidate;
                        return true;
                    }
                }
            }

            suppressCall = null;
            return false;
        }

        internal static bool TryFindDisposeBoolCall(BaseMethodDeclarationSyntax disposeMethod, [NotNullWhen(true)] out InvocationExpressionSyntax? suppressCall, [NotNullWhen(true)] out ArgumentSyntax? argument)
        {
            using (var walker = InvocationWalker.Borrow(disposeMethod))
            {
                foreach (var candidate in walker.Invocations)
                {
                    if (candidate.ArgumentList is { Arguments: { Count: 1 } arguments } &&
                        (argument = arguments[0]) is { Expression: { } expression } &&
                        expression.IsEither(SyntaxKind.TrueLiteralExpression, SyntaxKind.FalseLiteralExpression) &&
                        candidate.TryGetMethodName(out var name) &&
                        name == "Dispose")
                    {
                        suppressCall = candidate;
                        return true;
                    }
                }
            }

            suppressCall = null;
            argument = null;
            return false;
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

        internal static bool IsIDisposableDispose(IMethodSymbol candidate, Compilation compilation)
        {
            return candidate is { DeclaredAccessibility: Accessibility.Public, ReturnsVoid: true, Name: "Dispose", Parameters: { Length: 0 } } &&
                   Disposable.IsAssignableFrom(candidate.ContainingType, compilation);
        }
    }
}
