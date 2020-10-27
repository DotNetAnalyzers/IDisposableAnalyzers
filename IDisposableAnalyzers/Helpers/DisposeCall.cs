namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DisposeCall
    {
        internal static bool TryGetDisposed(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out IdentifierNameSyntax? disposedMember)
        {
            switch (disposeCall)
            {
                case { Expression: MemberAccessExpressionSyntax { Expression: { } expression } }
                    when TryGetName(expression, out var candidate):
                    return TryGetRoot(candidate, out disposedMember);
                case { Expression: MemberBindingExpressionSyntax _, Parent: ConditionalAccessExpressionSyntax { Expression: { } expression } }
                    when TryGetName(expression, out var candidate):
                    return TryGetRoot(candidate, out disposedMember);
            }

            disposedMember = null;
            return false;

            static bool TryGetName(ExpressionSyntax candidate, out IdentifierNameSyntax name)
            {
                switch (candidate)
                {
                    case ParenthesizedExpressionSyntax { Expression: { } expression }:
                        return TryGetName(expression, out name);
                    case CastExpressionSyntax { Expression: { } expression }:
                        return TryGetName(expression, out name);
                    case PostfixUnaryExpressionSyntax { Operand: { } expression, OperatorToken: { ValueText: "!" } }:
                        return TryGetName(expression, out name);
                    case BinaryExpressionSyntax { Left: { } expression, OperatorToken: { ValueText: "as" } }:
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
                    case IPropertySymbol { GetMethod: { DeclaringSyntaxReferences: { Length: 1 } } getMethod }
                        when getMethod.TrySingleDeclaration(cancellationToken, out SyntaxNode? getterOrExpressionBody):
                        {
                            using var walker = ReturnValueWalker.Borrow(getterOrExpressionBody, ReturnValueSearch.Member, semanticModel, cancellationToken);
                            if (walker.ReturnValues.TrySingle(out var returnValue))
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

        internal static bool IsDisposing(InvocationExpressionSyntax disposeCall, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryGetDisposed(disposeCall, semanticModel, cancellationToken, out var disposed) &&
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
                    return walker.ReturnValues.TrySingle(out var returnValue) &&
                           MemberPath.TrySingle(returnValue, out var expression) &&
                           semanticModel.TryGetSymbol(expression, cancellationToken, out ISymbol? nested) &&
                           nested.Equals(symbol);
                }
            }

            return false;
        }

        internal static bool IsMatchAny(InvocationExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return candidate.ArgumentList is { Arguments: { Count: 0 } } &&
                   (candidate.IsSymbol(KnownSymbol.IDisposable.Dispose, semanticModel, cancellationToken) ||
                    candidate.IsSymbol(KnownSymbol.IAsyncDisposable.DisposeAsync, semanticModel, cancellationToken));
        }
    }
}
