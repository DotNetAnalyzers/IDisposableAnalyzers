namespace IDisposableAnalyzers
{
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class AsyncAwait
    {
        internal static InvocationExpressionSyntax? FindAwaitedInvocation(AwaitExpressionSyntax awaitExpression)
        {
            return awaitExpression switch
            {
                { Expression: InvocationExpressionSyntax invocation }
                    when TryPeelConfigureAwait(invocation) is { } result
                    => result,
                { Expression: InvocationExpressionSyntax invocation } => invocation,
                _ => null,
            };
        }

        internal static ExpressionSyntax? AwaitTaskFromResult(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return expression switch
            {
                InvocationExpressionSyntax invocation
                    => AwaitTaskFromResult(invocation, semanticModel, cancellationToken),
                AwaitExpressionSyntax { Expression: { } awaited }
                    => AwaitTaskFromResult(awaited, semanticModel, cancellationToken),
                _ => null,
            };
        }

        internal static ExpressionSyntax? AwaitTaskFromResult(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryPeelConfigureAwait(invocation) is { } inner)
            {
                invocation = inner;
            }

            if (invocation is { ArgumentList: { Arguments: { Count: 1 } arguments } } &&
                arguments[0].Expression is { } expression &&
                invocation.IsSymbol(KnownSymbol.Task.FromResult, semanticModel, cancellationToken))
            {
                return expression;
            }

            return null;
        }

        internal static ExpressionSyntax? AwaitTaskRun(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return expression switch
            {
                InvocationExpressionSyntax invocation
                    => AwaitTaskRun(invocation, semanticModel, cancellationToken),
                AwaitExpressionSyntax { Expression: { } awaited }
                    => AwaitTaskRun(awaited, semanticModel, cancellationToken),
                _ => null,
            };
        }

        internal static ExpressionSyntax? AwaitTaskRun(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryPeelConfigureAwait(invocation) is { } inner)
            {
                invocation = inner;
            }

            if (invocation is { ArgumentList: { Arguments: { } arguments } } &&
                arguments.Count > 0 &&
                arguments[0].Expression is { } expression &&
                invocation.IsSymbol(KnownSymbol.Task.Run, semanticModel, cancellationToken))
            {
                return expression;
            }

            return null;
        }

        internal static InvocationExpressionSyntax? TryPeelConfigureAwait(InvocationExpressionSyntax invocation)
        {
            if (invocation is { ArgumentList: { Arguments: { Count: 1 } } } &
                invocation.TryGetMethodName(out var name) &&
                name == KnownSymbol.Task.ConfigureAwait.Name)
            {
                return TryPeel(invocation.Expression);

                static InvocationExpressionSyntax? TryPeel(ExpressionSyntax e)
                {
                    return e switch
                    {
                        InvocationExpressionSyntax inner => inner,
                        MemberAccessExpressionSyntax memberAccess => TryPeel(memberAccess.Expression),
                        _ => null,
                    };
                }
            }

            return null;
        }
    }
}
