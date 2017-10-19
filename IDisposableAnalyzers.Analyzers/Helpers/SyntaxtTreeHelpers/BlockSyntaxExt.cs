namespace IDisposableAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BlockSyntaxExt
    {
        internal static bool TryGetReturnExpression(this BlockSyntax body, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax returnValue)
        {
            return ReturnValueWalker.TryGetSingle(body, semanticModel, cancellationToken, out returnValue);
        }

        internal static bool TryGetAssignment(this BlockSyntax body, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax result)
        {
            result = null;
            if (symbol == null)
            {
                return false;
            }

            return AssignmentWalker.FirstWith(symbol, body, Search.TopLevel, semanticModel, cancellationToken, out result);
        }
    }
}