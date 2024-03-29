﻿namespace IDisposableAnalyzers;

using System.Threading;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal readonly struct DisposeCall
{
    internal readonly InvocationExpressionSyntax Invocation;

    internal DisposeCall(InvocationExpressionSyntax invocation)
    {
        this.Invocation = invocation;
    }

    internal static DisposeCall? MatchAny(InvocationExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return MatchDispose(candidate, semanticModel, cancellationToken) ??
               MatchDisposeAsync(candidate, semanticModel, cancellationToken);
    }

    internal static DisposeCall? MatchDispose(InvocationExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return candidate.ArgumentList is { Arguments.Count: 0 } &&
               candidate.IsSymbol(KnownSymbols.IDisposable.Dispose, semanticModel, cancellationToken)
         ? new DisposeCall(candidate)
         : (DisposeCall?)null;
    }

    internal static DisposeCall? MatchDisposeAsync(InvocationExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return candidate.ArgumentList is { Arguments.Count: 0 } &&
               candidate.IsSymbol(KnownSymbols.IAsyncDisposable.DisposeAsync, semanticModel, cancellationToken)
            ? new DisposeCall(candidate)
            : (DisposeCall?)null;
    }

    internal IdentifierNameSyntax? FindDisposed(SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return this.Invocation switch
        {
            { Expression: MemberAccessExpressionSyntax { Expression: { } expression } }
                when TryGetName(expression, out var candidate)
                => TryGetRoot(candidate, out var disposedMember) ? disposedMember : null,
            { Expression: MemberBindingExpressionSyntax _, Parent: ConditionalAccessExpressionSyntax { Expression: { } expression } }
                when TryGetName(expression, out var candidate)
                => TryGetRoot(candidate, out var disposedMember) ? disposedMember : null,
            _ => null,
        };

        static bool TryGetName(ExpressionSyntax candidate, out IdentifierNameSyntax name)
        {
            switch (candidate)
            {
                case ParenthesizedExpressionSyntax { Expression: { } expression }:
                    return TryGetName(expression, out name);
                case CastExpressionSyntax { Expression: { } expression }:
                    return TryGetName(expression, out name);
                case PostfixUnaryExpressionSyntax { Operand: { } expression, OperatorToken.ValueText: "!" }:
                    return TryGetName(expression, out name);
                case BinaryExpressionSyntax { Left: { } expression, OperatorToken.ValueText: "as" }:
                    return TryGetName(expression, out name);
                case IdentifierNameSyntax identifierName:
                    name = identifierName;
                    return true;
                case MemberAccessExpressionSyntax { Expression: { }, Name: IdentifierNameSyntax identifierName }:
                    name = identifierName;
                    return true;
                case MemberBindingExpressionSyntax { Name: IdentifierNameSyntax identifierName }:
                    name = identifierName;
                    return true;
                default:
                    name = null!;
                    return false;
            }
        }

        bool TryGetRoot(IdentifierNameSyntax expression, out IdentifierNameSyntax root)
        {
            switch (semanticModel.GetSymbolSafe(expression, cancellationToken))
            {
                case IPropertySymbol { GetMethod: null }:
                    root = null!;
                    return false;
                case IPropertySymbol property
                    when property.IsAbstract || property.IsAutoProperty():
                    root = expression;
                    return true;
                case IPropertySymbol { GetMethod: { DeclaringSyntaxReferences.Length: 1 } getMethod }
                    when getMethod.TrySingleDeclaration(cancellationToken, out SyntaxNode? getterOrExpressionBody):
                    {
                        using var walker = ReturnValueWalker.Borrow(getterOrExpressionBody, ReturnValueSearch.Member, semanticModel, cancellationToken);
                        if (walker.Values.TrySingle(out var returnValue))
                        {
                            switch (returnValue)
                            {
                                case IdentifierNameSyntax name:
                                    root = name;
                                    return true;
                                case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _, Name: IdentifierNameSyntax name }:
                                    root = name;
                                    return true;
                                case MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _, Name: IdentifierNameSyntax name } }:
                                    root = name;
                                    return true;
                            }
                        }
                    }

                    root = null!;
                    return false;

                default:
                    root = expression;
                    return true;
            }
        }
    }

    internal bool IsDisposing(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (this.FindDisposed(semanticModel, cancellationToken) is { } disposed &&
            semanticModel.TryGetSymbol(disposed, cancellationToken, out var disposedSymbol))
        {
            if (disposedSymbol.IsEquivalentTo(symbol))
            {
                return true;
            }

            if (disposedSymbol is IPropertySymbol property &&
                property.TrySingleDeclaration(cancellationToken, out var declaration))
            {
                using var walker = ReturnValueWalker.Borrow(declaration, ReturnValueSearch.Member, semanticModel, cancellationToken);
                return walker.Values.TrySingle(out var returnValue) &&
                       MemberPath.TrySingle(returnValue, out var expression) &&
                       semanticModel.TryGetSymbol(expression, cancellationToken, out ISymbol? nested) &&
                       SymbolComparer.Equal(nested, symbol);
            }
        }

        return false;
    }
}
