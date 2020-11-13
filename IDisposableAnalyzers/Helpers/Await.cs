namespace IDisposableAnalyzers
{
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Await
    {
        internal static InvocationExpressionSyntax? FindAwaitedInvocation(AwaitExpressionSyntax awaitExpression)
        {
            return awaitExpression switch
            {
                { Expression: InvocationExpressionSyntax invocation }
                    when PeelConfigureAwait(invocation) is InvocationExpressionSyntax result
                    => result,
                { Expression: InvocationExpressionSyntax invocation } => invocation,
                _ => null,
            };
        }

        internal static ExpressionSyntax? TaskFromResult(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (PeelConfigureAwait(invocation) is { } inner)
            {
                if (inner is InvocationExpressionSyntax innerInvocation)
                {
                    invocation = innerInvocation;
                }
                else
                {
                    return null;
                }
            }

            if (invocation is { ArgumentList: { Arguments: { Count: 1 } arguments } } &&
                arguments[0].Expression is { } expression &&
                invocation.IsSymbol(KnownSymbol.Task.FromResult, semanticModel, cancellationToken))
            {
                return expression;
            }

            return null;
        }

        internal static ParenthesizedLambdaExpressionSyntax? TaskRun(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (PeelConfigureAwait(invocation) is { } inner)
            {
                if (inner is InvocationExpressionSyntax innerInvocation)
                {
                    invocation = innerInvocation;
                }
                else
                {
                    return null;
                }
            }

            if (invocation is { ArgumentList: { Arguments: { } arguments } } &&
                arguments.Count > 0 &&
                arguments[0].Expression is ParenthesizedLambdaExpressionSyntax lambda &&
                invocation.IsSymbol(KnownSymbol.Task.Run, semanticModel, cancellationToken))
            {
                return lambda;
            }

            return null;
        }

        internal static ExpressionSyntax? PeelConfigureAwait(InvocationExpressionSyntax invocation)
        {
            if (invocation is { ArgumentList: { Arguments: { Count: 1 } } } &
                invocation.TryGetMethodName(out var name) &&
                name == KnownSymbol.Task.ConfigureAwait.Name)
            {
                return TryPeel(invocation.Expression);

                static ExpressionSyntax? TryPeel(ExpressionSyntax e)
                {
                    return e switch
                    {
                        MemberAccessExpressionSyntax memberAccess => TryPeel(memberAccess.Expression),
                        _ => e,
                    };
                }
            }

            return null;
        }
    }
}
