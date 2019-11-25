namespace IDisposableAnalyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        internal static bool Assigns(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken, out FieldOrProperty first)
        {
            if (localOrParameter.TryGetScope(cancellationToken, out var scope))
            {
                using var recursion = Recursion.Borrow(scope, semanticModel, cancellationToken);
                return Assigns(new Target<SyntaxNode, ISymbol, SyntaxNode>(null!, localOrParameter.Symbol, scope), recursion, out first);
            }

            return false;
        }

        internal static bool Assigns(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, out FieldOrProperty first)
        {
            using var recursion = Recursion.Borrow(candidate, semanticModel, cancellationToken);
            return Assigns(candidate, recursion, out first);
        }

        private static bool Assigns<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, Recursion recursion, out FieldOrProperty fieldOrProperty)
              where TSource : SyntaxNode
              where TSymbol : class, ISymbol
              where TNode : SyntaxNode
        {
            using (var walker = CreateUsagesWalker(target, recursion))
            {
                foreach (var usage in walker.usages)
                {
                    if (Assigns(usage, recursion, out fieldOrProperty))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Assigns(ExpressionSyntax candidate, Recursion recursion, out FieldOrProperty fieldOrProperty)
        {
            return candidate switch
            {
                { }
                when Identity(candidate, recursion) is { } id &&
                     Assigns(id, recursion, out fieldOrProperty)
                => true,
                { Parent: AssignmentExpressionSyntax { Left: { } left, Right: { } right } }
                => right.Contains(candidate) &&
                   recursion.SemanticModel.TryGetSymbol(left, recursion.CancellationToken, out var assignedSymbol) &&
                   FieldOrProperty.TryCreate(assignedSymbol, out fieldOrProperty),
                { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax _ } } argument }
                => recursion.Target(argument) is { } target &&
                   Assigns(target, recursion, out fieldOrProperty) &&
                   recursion.ContainingType.IsAssignableTo(fieldOrProperty.Symbol.ContainingType, recursion.SemanticModel.Compilation),
                { Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator } }
                => recursion.Target(variableDeclarator) is { } target &&
                   Assigns(target, recursion, out fieldOrProperty),
                _ => false,
            };
        }
    }
}
