namespace IDisposableAnalyzers
{
    using System;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    internal static class IDisposableFactory
    {
        private static readonly TypeSyntax IDisposable = SyntaxFactory.ParseTypeName("System.IDisposable")
                                                                      .WithSimplifiedNames();

        private static readonly IdentifierNameSyntax Dispose = SyntaxFactory.IdentifierName("Dispose");

        internal static ExpressionStatementSyntax DisposeStatement(ExpressionSyntax disposable)
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        disposable,
                        Dispose)));
        }

        internal static ExpressionStatementSyntax ConditionalDisposeStatement(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.ConditionalAccessExpression(
                    Normalize(MemberAccess(disposable)),
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(SyntaxFactory.Token(SyntaxKind.DotToken), Dispose))));

            ExpressionSyntax MemberAccess(ExpressionSyntax e)
            {
                switch (e)
                {
                    case { Parent: ArgumentSyntax { RefOrOutKeyword: { } refOrOut } }
                        when !refOrOut.IsKind(SyntaxKind.None):
                        return e;
                    case IdentifierNameSyntax _:
                    case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _, Name: { } } _:
                        if (semanticModel.GetSymbolInfo(e, cancellationToken).Symbol is IPropertySymbol { GetMethod: { } get } &&
                            get.TrySingleAccessorDeclaration(cancellationToken, out var getter))
                        {
                            switch (getter)
                            {
                                case { ExpressionBody: { Expression: { } expression } }:
                                    return expression;
                                case { Body: { Statements: { Count: 1 } statements } }
                                    when statements[0] is ReturnStatementSyntax { Expression: { } expression }:
                                    return expression;
                            }
                        }

                        return e;
                    default:
                        return e;
                }
            }

            ExpressionSyntax Normalize(ExpressionSyntax e)
            {
                if (semanticModel.ClassifyConversion(e, KnownSymbol.IDisposable.GetTypeSymbol(semanticModel.Compilation)).IsImplicit)
                {
                    if (semanticModel.TryGetType(e, cancellationToken, out var type) &&
                        DisposeMethod.TryFindIDisposableDispose(type, semanticModel.Compilation, Search.Recursive, out var disposeMethod) &&
                        disposeMethod.ExplicitInterfaceImplementations.IsEmpty)
                    {
                        return e.WithoutTrivia()
                                .WithLeadingElasticLineFeed();
                    }

                    return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(IDisposable, e));
                }

                return AsIDisposable(e.WithoutTrivia())
                    .WithLeadingElasticLineFeed();
            }
        }

        internal static AnonymousFunctionExpressionSyntax PrependStatements(this AnonymousFunctionExpressionSyntax lambda, params StatementSyntax[] statements)
        {
            return lambda switch
            {
                { Body: ExpressionSyntax body } => lambda.ReplaceNode(
                                                             body,
                                                             SyntaxFactory.Block(statements.Append(SyntaxFactory.ExpressionStatement(body)))
                                                                          .WithLeadingLineFeed())
                                                         .WithAdditionalAnnotations(Formatter.Annotation),
                { Body: BlockSyntax block } => lambda.ReplaceNode(block, block.AddStatements(statements)),
                _ => throw new NotSupportedException(
                    $"No support for adding statements to lambda with the shape: {lambda?.ToString() ?? "null"}"),
            };
        }

        internal static MethodDeclarationSyntax AsBlockBody(this MethodDeclarationSyntax method, params StatementSyntax[] statements)
        {
            return method.WithExpressionBody(null)
                         .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                         .WithBody(SyntaxFactory.Block(statements));
        }

        internal static ParenthesizedExpressionSyntax AsIDisposable(ExpressionSyntax e)
        {
            return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, e, IDisposable));
        }
    }
}
