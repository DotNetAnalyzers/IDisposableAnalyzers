namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsDisposedBefore(ISymbol symbol, ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (TryGetScope(expression, out var block))
            {
                using var walker = InvocationWalker.Borrow(block);
                foreach (var invocation in walker.Invocations)
                {
                    if (invocation.IsExecutedBefore(expression) == ExecutedBefore.No)
                    {
                        continue;
                    }

                    if (DisposeCall.IsDisposing(invocation, symbol, semanticModel, cancellationToken) &&
                        !IsReassignedAfter(block, invocation))
                    {
                        return true;
                    }
                }
            }

            if (expression is AssignmentExpressionSyntax { Left: { } left } &&
                semanticModel.GetSymbolSafe(left, cancellationToken) is IPropertySymbol property &&
                property.TryGetSetter(cancellationToken, out var setter))
            {
                using var pooled = InvocationWalker.Borrow(setter);
                foreach (var invocation in pooled.Invocations)
                {
                    if ((DisposeCall.IsDisposing(invocation, symbol, semanticModel, cancellationToken) ||
                         DisposeCall.IsDisposing(invocation, property, semanticModel, cancellationToken)) &&
                         !IsReassignedAfter(setter, invocation))
                    {
                        return true;
                    }
                }
            }

            return false;

            static bool TryGetScope(SyntaxNode node, out BlockSyntax result)
            {
                if (node.FirstAncestor<AnonymousFunctionExpressionSyntax>() is { Body: BlockSyntax lambdaBody })
                {
                    result = lambdaBody;
                    return true;
                }

                if (node.FirstAncestor<AccessorDeclarationSyntax>() is { Body: BlockSyntax accessorBody })
                {
                    result = accessorBody;
                    return true;
                }

                if (node.FirstAncestor<BaseMethodDeclarationSyntax>() is { Body: BlockSyntax methodBody })
                {
                    result = methodBody;
                    return true;
                }

                result = null!;
                return false;
            }

            bool IsReassignedAfter(SyntaxNode scope, InvocationExpressionSyntax disposeCall)
            {
                using var walker = AssignmentWalker.Borrow(scope);
                foreach (var assignment in walker.Assignments)
                {
                    if (assignment.TryFirstAncestor(out StatementSyntax? statement) &&
                        disposeCall.IsExecutedBefore(statement) == ExecutedBefore.Yes &&
                        statement.IsExecutedBefore(expression) == ExecutedBefore.Yes &&
                        semanticModel.TryGetSymbol(assignment.Left, cancellationToken, out var assigned) &&
                        Equals(assigned, symbol) &&
                        IsCreation(assignment.Right, semanticModel, cancellationToken).IsEither(Result.Yes, Result.AssumeYes))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
