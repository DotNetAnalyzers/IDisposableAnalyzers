namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class AsyncAwait
    {
        internal static bool TryGetAwaitedInvocation(AwaitExpressionSyntax @await, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax result)
        {
            result = null;
            if (@await?.Expression == null)
            {
                return false;
            }

            if (TryPeelConfigureAwait(@await.Expression as InvocationExpressionSyntax, semanticModel, cancellationToken, out result))
            {
                return result != null;
            }

            result = @await.Expression as InvocationExpressionSyntax;
            return result != null;
        }

        internal static bool TryAwaitTaskFromResult(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax result)
        {
            if (expression is InvocationExpressionSyntax invocation)
            {
                return TryAwaitTaskFromResult(invocation, semanticModel, cancellationToken, out result);
            }

            if (expression is AwaitExpressionSyntax @await)
            {
                return TryAwaitTaskFromResult(@await.Expression, semanticModel, cancellationToken, out result);
            }

            result = null;
            return false;
        }

        internal static bool TryAwaitTaskFromResult(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax result)
        {
            result = null;
            if (TryPeelConfigureAwait(invocation, semanticModel, cancellationToken, out InvocationExpressionSyntax inner))
            {
                invocation = inner;
            }

            if (invocation?.ArgumentList == null ||
                invocation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var symbol = semanticModel.GetSymbolSafe(invocation, cancellationToken);
            if (symbol == KnownSymbol.Task.FromResult)
            {
                result = invocation.ArgumentList.Arguments[0].Expression;
            }

            return result != null;
        }

        internal static bool TryAwaitTaskRun(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax result)
        {
            if (expression is InvocationExpressionSyntax invocation)
            {
                return TryAwaitTaskRun(invocation, semanticModel, cancellationToken, out result);
            }

            if (expression is AwaitExpressionSyntax @await)
            {
                return TryAwaitTaskRun(@await.Expression, semanticModel, cancellationToken, out result);
            }

            result = null;
            return false;
        }

        internal static bool TryAwaitTaskRun(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax result)
        {
            result = null;
            if (TryPeelConfigureAwait(invocation, semanticModel, cancellationToken, out InvocationExpressionSyntax inner))
            {
                invocation = inner;
            }

            if (invocation?.ArgumentList == null ||
                invocation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var symbol = semanticModel.GetSymbolSafe(invocation, cancellationToken);
            if (symbol == KnownSymbol.Task.Run)
            {
                result = invocation.ArgumentList.Arguments[0].Expression;
            }

            return result != null;
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

                var memberaccess = invocation?.Expression as MemberAccessExpressionSyntax;
                while (memberaccess != null)
                {
                    result = memberaccess.Expression as InvocationExpressionSyntax;
                    if (result != null)
                    {
                        return true;
                    }

                    memberaccess = memberaccess.Expression as MemberAccessExpressionSyntax;
                }
            }

            return false;
        }
    }
}
