namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class InvocationExpressionSyntaxExt
    {
        internal static bool TryGetMatchingParameter(this InvocationExpressionSyntax invocation, ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out IParameterSymbol parameter)
        {
            parameter = null;
            if (invocation?.ArgumentList == null)
            {
                return false;
            }

            if (semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol method)
            {
                if (argument.NameColon is NameColonSyntax nameColon &&
                    nameColon.Name is IdentifierNameSyntax name)
                {
                    return method.Parameters.TrySingle(x => x.Name == name.Identifier.ValueText, out parameter);
                }

                return method.Parameters.TryElementAt(
                    invocation.ArgumentList.Arguments.IndexOf(argument),
                    out parameter);
            }

            return false;
        }
    }
}
