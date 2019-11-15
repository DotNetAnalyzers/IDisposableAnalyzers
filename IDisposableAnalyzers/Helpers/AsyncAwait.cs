namespace IDisposableAnalyzers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class AsyncAwait
    {
        internal static bool TryGetAwaitedInvocation(AwaitExpressionSyntax awaitExpression, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out InvocationExpressionSyntax? result)
        {
            switch (awaitExpression)
            {
                case { Expression: InvocationExpressionSyntax invocation }
                    when TryPeelConfigureAwait(invocation, semanticModel, cancellationToken, out result):
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
                case AwaitExpressionSyntax { Expression: { } awaited }:
                    return TryAwaitTaskRun(awaited, semanticModel, cancellationToken, out result);
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

        internal static bool TryPeelConfigureAwait(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out InvocationExpressionSyntax? result)
        {
            if (invocation is { ArgumentList: { Arguments: { Count: 1 } } } &
                invocation.TryGetMethodName(out var name) &&
                name == KnownSymbol.Task.ConfigureAwait.Name)
            {
                result = invocation.Expression as InvocationExpressionSyntax;
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

            result = null;
            return false;
        }
    }
}
