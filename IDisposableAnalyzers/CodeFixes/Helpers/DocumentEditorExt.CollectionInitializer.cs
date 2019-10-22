namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;

    internal static partial class DocumentEditorExt
    {
        internal static void AddItemToCollectionInitializer(this DocumentEditor editor, ObjectCreationExpressionSyntax objectCreation, ExpressionSyntax expression, SyntaxTriviaList trivia)
        {
            if (objectCreation.Initializer != null ||
                trivia.Any(SyntaxKind.SingleLineCommentTrivia))
            {
                editor.ReplaceNode(objectCreation, (x, _) => ComputeReplacement(x));

                SyntaxNode ComputeReplacement(SyntaxNode x)
                {
                    var oc = ((ObjectCreationExpressionSyntax)x).RemoveEmptyArgumentList();
                    if (oc.Initializer != null &&
                        oc.Initializer.Expressions.Count > 0)
                    {
                        var last = oc.Initializer.Expressions.Last();
                        var updatedExpressions = oc.Initializer.Expressions
                                                               .Remove(last)
                                                               .Add(last.WithoutTrailingTrivia())
                                                               .GetWithSeparators()
                                                               .Add(SyntaxFactory.Token(SyntaxKind.CommaToken)
                                                                                 .WithTrailingTrivia(last.GetTrailingTrivia()))
                                                               .Add(expression.WithoutTrailingTrivia())
                                                               .Add(SyntaxFactory.Token(SyntaxKind.CommaToken)
                                                                                 .WithTrailingTrivia(trivia));
                        return oc.WithInitializer(
                            SyntaxFactory.InitializerExpression(
                                            SyntaxKind.CollectionInitializerExpression,
                                            SyntaxFactory.SeparatedList<ExpressionSyntax>(updatedExpressions))
                                        .WithOpenBraceToken(
                                            SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                                                         .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)))
                                        .WithAdditionalAnnotations(Formatter.Annotation);
                    }
                    else
                    {
                        var updatedExpressions = SyntaxFactory.NodeOrTokenList(
                            expression.WithoutTrailingTrivia(),
                            SyntaxFactory.Token(SyntaxKind.CommaToken)
                                         .WithTrailingTrivia(trivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed)));
                        return oc.WithInitializer(
                            SyntaxFactory.InitializerExpression(
                                             SyntaxKind.CollectionInitializerExpression,
                                             SyntaxFactory.SeparatedList<ExpressionSyntax>(updatedExpressions))
                                         .WithOpenBraceToken(
                                             SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                                                          .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)));
                    }
                }
            }
            else
            {
                editor.ReplaceNode(objectCreation, (x, _) => ComputeReplacement(x));

                SyntaxNode ComputeReplacement(SyntaxNode x)
                {
                    return ((ObjectCreationExpressionSyntax)x).RemoveEmptyArgumentList()
                                                              .WithInitializer(
                                                                  SyntaxFactory.InitializerExpression(
                                                                      SyntaxKind.CollectionInitializerExpression,
                                                                      SyntaxFactory
                                                                          .SingletonSeparatedList(expression)));
                }
            }
        }

        private static ObjectCreationExpressionSyntax RemoveEmptyArgumentList(this ObjectCreationExpressionSyntax objectCreation)
        {
            if (objectCreation.ArgumentList != null &&
                objectCreation.ArgumentList.Arguments.Count == 0)
            {
                objectCreation = objectCreation.RemoveNode(objectCreation.ArgumentList, SyntaxRemoveOptions.KeepTrailingTrivia);
            }

            return objectCreation;
        }
    }
}
