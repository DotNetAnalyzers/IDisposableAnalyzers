namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ObjectCreationExt
    {
        internal static bool Creates(this ObjectCreationExpressionSyntax creation, ConstructorDeclarationSyntax ctor, Search search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var created = semanticModel.GetSymbolSafe(creation, cancellationToken) as IMethodSymbol;
            var ctorSymbol = semanticModel.GetDeclaredSymbolSafe(ctor, cancellationToken);
            if (SymbolComparer.Equals(ctorSymbol, created))
            {
                return true;
            }

            return search == Search.Recursive &&
                   Constructor.IsRunBefore(created, ctorSymbol, semanticModel, cancellationToken);
        }

        internal static bool TryGetMatchingParameter(this ObjectCreationExpressionSyntax objectCreation, ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out IParameterSymbol parameter)
        {
            parameter = null;
            if (objectCreation?.ArgumentList == null)
            {
                return false;
            }

            if (semanticModel.GetSymbolSafe(objectCreation, cancellationToken) is IMethodSymbol method)
            {
                if (argument.NameColon is NameColonSyntax nameColon &&
                    nameColon.Name is IdentifierNameSyntax name)
                {
                    return method.Parameters.TrySingle(x => x.Name == name.Identifier.ValueText, out parameter);
                }

                return method.Parameters.TryElementAt(
                    objectCreation.ArgumentList.Arguments.IndexOf(argument),
                    out parameter);
            }

            return false;
        }

        internal static bool TryGetMatchingArgument(this ObjectCreationExpressionSyntax objectCreation, IParameterSymbol parameter, out ArgumentSyntax argument)
        {
            if (objectCreation?.ArgumentList == null ||
                parameter == null)
            {
                argument = null;
                return false;
            }

            return objectCreation.ArgumentList.TryGetMatchingArgument(parameter, out argument);
        }
    }
}
