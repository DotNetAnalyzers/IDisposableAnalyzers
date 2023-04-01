namespace IDisposableAnalyzers
{
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class GC
    {
        internal readonly struct SuppressFinalize
        {
            internal readonly InvocationExpressionSyntax Invocation;

            internal SuppressFinalize(InvocationExpressionSyntax invocation)
            {
                this.Invocation = invocation;
            }

            internal ArgumentSyntax Argument => this.Invocation.ArgumentList.Arguments[0];

            internal static SuppressFinalize? Match(InvocationExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (candidate.ArgumentList is { Arguments.Count: 1 } &&
                    candidate.TryGetMethodName(out var name) &&
                    name == "SuppressFinalize" &&
                    semanticModel.TryGetSymbol(candidate, KnownSymbols.GC.SuppressFinalize, cancellationToken, out _))
                {
                    return new SuppressFinalize(candidate);
                }

                return null;
            }

            internal static SuppressFinalize? Find(MethodDeclarationSyntax disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                using var walker = InvocationWalker.Borrow(disposeMethod);
                foreach (var candidate in walker.Invocations)
                {
                    if (Match(candidate, semanticModel, cancellationToken) is { } match)
                    {
                        return match;
                    }
                }

                return null;
            }
        }
    }
}
