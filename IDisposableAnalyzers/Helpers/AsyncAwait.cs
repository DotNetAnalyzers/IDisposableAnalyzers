namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class AsyncAwait
    {
        internal static bool TryGetAwaitedInvocation(AwaitExpressionSyntax awaitExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax result)
        {
            result = null;
            if (awaitExpression?.Expression == null)
            {
                return false;
            }

            if (TryPeelConfigureAwait(awaitExpression.Expression as InvocationExpressionSyntax, semanticModel, cancellationToken, out result))
            {
                return result != null;
            }

            result = awaitExpression.Expression as InvocationExpressionSyntax;
            return result != null;
        }

        internal static bool TryAwaitTaskFromResult(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ExpressionSyntax? result)
        {
            switch (expression)
            {
                case InvocationExpressionSyntax invocation:
                    return TryAwaitTaskFromResult(invocation, semanticModel, cancellationToken, out result);
                case AwaitExpressionSyntax awaitExpression:
                    return TryAwaitTaskFromResult(awaitExpression.Expression, semanticModel, cancellationToken, out result);
            }

            result = null;
            return false;
        }

        internal static bool TryAwaitTaskFromResult(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ExpressionSyntax? result)
        {
            if (TryPeelConfigureAwait(invocation, semanticModel, cancellationToken, out var inner))
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
                case AwaitExpressionSyntax awaitExpression:
                    return TryAwaitTaskRun(awaitExpression.Expression, semanticModel, cancellationToken, out result);
            }

            result = null;
            return false;
        }

        internal static bool TryAwaitTaskRun(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ExpressionSyntax? result)
        {
            if (TryPeelConfigureAwait(invocation, semanticModel, cancellationToken, out var inner))
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

        internal static bool TryPeelConfigureAwait(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax result)
        {
            result = null;
            var method = semanticModel.GetSymbolSafe(invocation, cancellationToken);
            if (method?.Name == "ConfigureAwait")
            {
                result = invocation?.Expression as InvocationExpressionSyntax;
                if (result != null)
                {
                    return true;
                }

                var memberAccess = invocation?.Expression as MemberAccessExpressionSyntax;
                while (memberAccess != null)
                {
                    result = memberAccess.Expression as InvocationExpressionSyntax;
                    if (result != null)
                    {
                        return true;
                    }

                    memberAccess = memberAccess.Expression as MemberAccessExpressionSyntax;
                }
            }

            return false;
        }
    }
}
