namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ArgumentListSyntaxExt
    {
        internal static bool TryGetMatchingArgument(this ArgumentListSyntax arguments, IParameterSymbol parameter, out ArgumentSyntax argument)
        {
            argument = null;
            if (parameter == null ||
                arguments == null ||
                arguments.Arguments.Count == 0)
            {
                return false;
            }

            foreach (var candidate in arguments.Arguments)
            {
                if (candidate.NameColon?.Name?.Identifier.ValueText == parameter.Name)
                {
                    argument = candidate;
                    return true;
                }
            }

            if (arguments.Arguments.Count <= parameter.Ordinal ||
                parameter.Ordinal == -1)
            {
                return false;
            }

            argument = arguments.Arguments[parameter.Ordinal];
            return true;
        }

        internal static bool TryGetArgumentValue(this ArgumentListSyntax arguments, IParameterSymbol parameter, CancellationToken cancellationToken, out ExpressionSyntax value)
        {
            value = null;
            if (arguments == null ||
                parameter == null)
            {
                return false;
            }

            if (TryGetMatchingArgument(arguments, parameter, out var argument))
            {
                value = argument.Expression;
                return value != null;
            }

            if (parameter.HasExplicitDefaultValue && parameter.TrySingleDeclaration(cancellationToken, out SyntaxNode declaration))
            {
                value = (declaration as ParameterSyntax)?.Default.Value;
            }

            return value != null;
        }
    }
}
