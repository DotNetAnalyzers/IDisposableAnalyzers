namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class InvocationExpressionSyntaxExt
    {
        internal static string Name(this InvocationExpressionSyntax invocation)
        {
            if (invocation == null)
            {
                return null;
            }

            switch (invocation.Kind())
            {
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.TypeOfExpression:
                    var identifierName = invocation.Expression as IdentifierNameSyntax;
                    if (identifierName == null)
                    {
                        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                        {
                            identifierName = memberAccess.Name as IdentifierNameSyntax;
                        }
                    }

                    return identifierName?.Identifier.ValueText;
                default:
                    return null;
            }
        }

        internal static bool IsNameOf(this InvocationExpressionSyntax invocation)
        {
            return invocation.Name() == "nameof";
        }

        internal static bool TryGetArgumentValue(this InvocationExpressionSyntax invocation, IParameterSymbol parameter, CancellationToken cancellationToken, out ExpressionSyntax value)
        {
            if (invocation?.ArgumentList == null ||
                parameter == null)
            {
                value = null;
                return false;
            }

            return invocation.ArgumentList.TryGetArgumentValue(parameter, cancellationToken, out value);
        }

        internal static bool TryGetMatchingParameter(this InvocationExpressionSyntax invocation, ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out IParameterSymbol parameter)
        {
            parameter = null;
            if (invocation?.ArgumentList == null)
            {
                return false;
            }

            if (semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol method)
            {
                foreach (var reference in method.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax(cancellationToken) is MethodDeclarationSyntax methodDeclaration)
                    {
                        if (methodDeclaration.TryGetMatchingParameter(argument, out ParameterSyntax parameterSyntax))
                        {
                            parameter = semanticModel.GetDeclaredSymbolSafe(parameterSyntax, cancellationToken);
                            return parameter != null;
                        }
                    }
                }
            }

            return false;
        }
    }
}