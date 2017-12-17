namespace IDisposableAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Snippet
    {
        internal static StatementSyntax DisposeStatement(ISymbol member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var prefix = member.IsEither<IFieldSymbol, IPropertySymbol>() &&
                         !semanticModel.UsesUnderscore(cancellationToken)
                    ? "this."
                    : string.Empty;
            if (!Disposable.IsAssignableTo(MemberType(member)))
            {
                return SyntaxFactory.ParseStatement($"({prefix}{member.Name} as System.IDisposable)?.Dispose();")
                                    .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                    .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                                    .WithSimplifiedNames();
            }

            if (IsNeverNull(member, semanticModel, cancellationToken))
            {
                return SyntaxFactory.ParseStatement($"{prefix}{member.Name}.Dispose();")
                                    .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                    .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
            }

            return SyntaxFactory.ParseStatement($"{prefix}{member.Name}?.Dispose();")
                                .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
        }

        private static ITypeSymbol MemberType(ISymbol member) =>
            (member as IFieldSymbol)?.Type ??
            (member as IPropertySymbol)?.Type ??
            (member as ILocalSymbol)?.Type ??
            (member as IParameterSymbol)?.Type;

        private static bool IsNeverNull(ISymbol member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (member is IFieldSymbol field &&
                !field.IsReadOnly)
            {
                return false;
            }

            if (member is IPropertySymbol property &&
                !property.IsReadOnly)
            {
                return false;
            }

            using (var assignedValues = AssignedValueWalker.Borrow(member, semanticModel, cancellationToken))
            {
                foreach (var value in assignedValues)
                {
                    if (value is ObjectCreationExpressionSyntax objectCreation)
                    {
                        if (objectCreation.Parent is EqualsValueClauseSyntax equalsValueClause &&
                            equalsValueClause.Parent is VariableDeclaratorSyntax)
                        {
                            continue;
                        }

                        if (objectCreation.Parent is AssignmentExpressionSyntax assignment &&
                            assignment.Parent is ExpressionStatementSyntax statement &&
                            statement.Parent is BlockSyntax block &&
                            block.Parent is ConstructorDeclarationSyntax)
                        {
                            continue;
                        }
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
