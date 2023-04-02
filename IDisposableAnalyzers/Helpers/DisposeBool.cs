namespace IDisposableAnalyzers;

using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal readonly struct DisposeBool
{
    internal readonly InvocationExpressionSyntax Invocation;

    internal DisposeBool(InvocationExpressionSyntax invocation)
    {
        this.Invocation = invocation;
    }

    internal ArgumentSyntax Argument => this.Invocation.ArgumentList.Arguments[0];

    internal static DisposeBool? Match(InvocationExpressionSyntax candidate)
    {
        if (candidate.ArgumentList is { Arguments: { Count: 1 } arguments } &&
            arguments[0] is { Expression: { } } &&
            candidate.TryGetMethodName(out var name) &&
            name == "Dispose")
        {
            return new DisposeBool(candidate);
        }

        return null;
    }

    internal static DisposeBool? Find(BaseMethodDeclarationSyntax disposeMethod)
    {
        using var walker = InvocationWalker.Borrow(disposeMethod);
        foreach (var candidate in walker.Invocations)
        {
            if (Match(candidate) is { } match)
            {
                return match;
            }
        }

        return null;
    }
}
