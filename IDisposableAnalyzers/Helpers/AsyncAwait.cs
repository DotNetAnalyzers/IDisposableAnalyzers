namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class AsyncAwait
    {
        internal static bool TryGetAwaitedInvocation(AwaitExpressionSyntax awaitExpression, [NotNullWhen(true)] out InvocationExpressionSyntax? result)
        {
            switch (awaitExpression)
            {
                case { Expression: InvocationExpressionSyntax invocation }
                    when TryPeelConfigureAwait(invocation, out result):
                    return true;
                case { Expression: InvocationExpressionSyntax invocation }:
                    result = invocation;
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        internal static bool TryAwaitTaskFromResult(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ExpressionSyntax? result)
        {
            switch (expression)
            {
                case InvocationExpressionSyntax invocation:
                    return TryAwaitTaskFromResult(invocation, semanticModel, cancellationToken, out result);
                case AwaitExpressionSyntax { Expression: { } awaited }:
                    return TryAwaitTaskFromResult(awaited, semanticModel, cancellationToken, out result);
            }

            result = null;
            return false;
        }

        internal static bool TryAwaitTaskFromResult(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ExpressionSyntax? result)
        {
            if (TryPeelConfigureAwait(invocation, out var inner))
            {
                invocation = inner;
            }

            if (invocation is { ArgumentList: { Arguments: { Count: 1 } arguments } } &&
                arguments[0].Expression is { } expression &&
                invocation.IsSymbol(KnownSymbol.Task.FromResult, semanticModel, cancellationToken))
            {
                result = expression;
                return true;
            }

            result = null;
            return false;
        }

        internal static bool TryAwaitTaskRun(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ExpressionSyntax? result)
        {
            switch (expression)
            {
                case InvocationExpressionSyntax invocation:
                    return TryAwaitTaskRun(invocation, semanticModel, cancellationToken, out result);
                case AwaitExpressionSyntax { Expression: { } awaited }:
                    return TryAwaitTaskRun(awaited, semanticModel, cancellationToken, out result);
            }

            result = null;
            return false;
        }

        internal static bool TryAwaitTaskRun(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ExpressionSyntax? result)
        {
            if (TryPeelConfigureAwait(invocation, out var inner))
            {
                invocation = inner;
            }

            if (invocation is { ArgumentList: { Arguments: { } arguments } } &&
                arguments.Count > 0 &&
                arguments[0].Expression is { } expression &&
                invocation.IsSymbol(KnownSymbol.Task.Run, semanticModel, cancellationToken))
            {
                result = expression;
                return true;
            }

            result = null;
            return false;
        }

        internal static bool TryPeelConfigureAwait(InvocationExpressionSyntax invocation, [NotNullWhen(true)] out InvocationExpressionSyntax? result)
        {
            if (invocation is { ArgumentList: { Arguments: { Count: 1 } } } &
                invocation.TryGetMethodName(out var name) &&
                name == KnownSymbol.Task.ConfigureAwait.Name)
            {
                return TryPeel(invocation.Expression, out result);

                bool TryPeel(ExpressionSyntax e, out InvocationExpressionSyntax peeled)
                {
                    switch (e)
                    {
                        case InvocationExpressionSyntax inner:
                            peeled = inner;
                            return true;
                        case MemberAccessExpressionSyntax memberAccess:
                            return TryPeel(memberAccess.Expression, out peeled);
                        default:
                            peeled = default;
                            return false;
                    }
                }
            }

            result = null;
            return false;
        }
    }
}
