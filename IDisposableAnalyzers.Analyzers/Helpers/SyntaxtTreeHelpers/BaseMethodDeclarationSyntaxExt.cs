namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BaseMethodDeclarationSyntaxExt
    {
        internal static bool TryGetMatchingParameter(this BaseMethodDeclarationSyntax method, ArgumentSyntax argument, out ParameterSyntax parameter)
        {
            parameter = null;
            if (argument == null ||
                method?.ParameterList == null)
            {
                return false;
            }

            if (argument.NameColon == null)
            {
                var index = argument.FirstAncestorOrSelf<ArgumentListSyntax>()
                                    .Arguments.IndexOf(argument);
                if (method.ParameterList.Parameters.TryElementAt(index, out parameter))
                {
                    return true;
                }

                parameter = method.ParameterList.Parameters.Last();
                foreach (var modifier in parameter.Modifiers)
                {
                    if (modifier.IsKind(SyntaxKind.ParamsKeyword))
                    {
                        return true;
                    }
                }

                parameter = null;
                return false;
            }

            foreach (var candidate in method.ParameterList.Parameters)
            {
                if (candidate.Identifier.ValueText == argument.NameColon.Name.Identifier.ValueText)
                {
                    parameter = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}