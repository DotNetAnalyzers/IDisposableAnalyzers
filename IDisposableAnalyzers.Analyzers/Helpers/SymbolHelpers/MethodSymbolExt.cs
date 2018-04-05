namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class MethodSymbolExt
    {
        internal static bool TryGetMatchingParameter(this IMethodSymbol method, ArgumentSyntax argument, out IParameterSymbol parameter)
        {
            if (argument.NameColon is NameColonSyntax nameColon &&
                nameColon.Name is IdentifierNameSyntax name)
            {
                return method.Parameters.TrySingle(x => x.Name == name.Identifier.ValueText, out parameter);
            }

            if (argument.Parent is ArgumentListSyntax argumentList)
            {
                return method.Parameters.TryElementAt(argumentList.Arguments.IndexOf(argument), out parameter);
            }

            parameter = null;
            return false;
        }
    }
}
