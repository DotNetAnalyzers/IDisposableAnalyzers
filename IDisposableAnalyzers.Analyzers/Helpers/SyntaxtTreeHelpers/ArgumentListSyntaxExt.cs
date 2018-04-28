namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ArgumentListSyntaxExt
    {
        internal static bool TryGetArgumentValue(this ArgumentListSyntax arguments, IParameterSymbol parameter, CancellationToken cancellationToken, out ExpressionSyntax value)
        {
            value = null;
            if (arguments == null ||
                parameter == null)
            {
                return false;
            }

            if (arguments.TryGetMatchingArgument(parameter, out var argument))
            {
                value = argument.Expression;
                return value != null;
            }

            if (parameter.HasExplicitDefaultValue && parameter.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration.Default is EqualsValueClauseSyntax equalsValueClause)
            {
                value = equalsValueClause.Value;
            }

            return value != null;
        }
    }
}
