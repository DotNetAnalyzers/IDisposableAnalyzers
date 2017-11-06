namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class InvocationExpressionSyntaxExt
    {
        internal static bool TryGetInvokedMethodName(this InvocationExpressionSyntax invocation, out string name)
        {
            name = null;
            if (invocation == null)
            {
                return false;
            }

            switch (invocation.Kind())
            {
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.TypeOfExpression:
                    if (invocation.Expression is SimpleNameSyntax simple)
                    {
                        name = simple.Identifier.ValueText;
                        return true;
                    }

                    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Name is SimpleNameSyntax member)
                    {
                        name = member.Identifier.ValueText;
                        return true;
                    }

                    if (invocation.Expression is MemberBindingExpressionSyntax memberBinding &&
                        memberBinding.Name is SimpleNameSyntax bound)
                    {
                        name = bound.Identifier.ValueText;
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        internal static bool IsNameOf(this InvocationExpressionSyntax invocation)
        {
            return invocation.TryGetInvokedMethodName(out var name) &&
                   name == "nameof";
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