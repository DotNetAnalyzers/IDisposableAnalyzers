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

        internal static ExpressionStatementSyntax ConditionalDisposeStatement(ExpressionSyntax disposable)
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.ConditionalAccessExpression(
                    disposable,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(SyntaxFactory.Token(SyntaxKind.DotToken), Dispose))));
        }

        internal static ExpressionStatementSyntax DisposeStatement(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
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

        internal static ExpressionStatementSyntax DisposeStatement(FieldOrProperty disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = MutationWalker.For(disposable, semanticModel, cancellationToken))
            {
                if (IsNeverNull(out var neverNull))
                {
                    if (disposable.Type.IsAssignableTo(KnownSymbol.IDisposable, semanticModel.Compilation) &&
                        DisposeMethod.TryFindIDisposableDispose(disposable.Type, semanticModel.Compilation, Search.Recursive, out var disposeMethod) &&
                        disposeMethod.ExplicitInterfaceImplementations.IsEmpty)
                    {
                        return DisposeStatement(neverNull.WithoutTrivia()).WithLeadingElasticLineFeed();
                    }

                    return DisposeStatement(
                        SyntaxFactory.CastExpression(
                            IDisposable,
                            neverNull.WithoutTrivia()))
                        .WithLeadingElasticLineFeed();
                }

                bool IsNeverNull(out ExpressionSyntax memberAccess)
                {
                    if (walker.TrySingle(out var mutation) &&
                        mutation is AssignmentExpressionSyntax { Left: { } single, Right: ObjectCreationExpressionSyntax _, Parent: ExpressionStatementSyntax { Parent: BlockSyntax { Parent: ConstructorDeclarationSyntax _ } } } &&
                        disposable.Symbol.ContainingType.Constructors.Length == 1)
                    {
                        memberAccess = single;
                        return true;
                    }

                    if (walker.IsEmpty &&
                        disposable.Initializer(cancellationToken) is { Value: ObjectCreationExpressionSyntax _ } initializer &&
                        initializer.TryFirstAncestor(out TypeDeclarationSyntax containingType))
                    {
                        if (TryGetMemberAccessFromUsage(containingType, out memberAccess))
                        {
                            return true;
                        }

                        switch (initializer.Parent)
                        {
                            case PropertyDeclarationSyntax { Identifier: { } identifier }:
                                memberAccess = Create(identifier);
                                return true;
                            case VariableDeclaratorSyntax { Identifier: { } identifier }:
                                memberAccess = Create(identifier);
                                return true;
                        }
                    }

                    memberAccess = null;
                    return false;
                }
            }

            if (DisposeMethod.IsAccessibleOn(disposable.Type, semanticModel.Compilation))
            {
                return ConditionalDisposeStatement(MemberAccess()).WithLeadingElasticLineFeed();
            }

            return ConditionalDisposeStatement(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.AsExpression,
                        MemberAccess(),
                        IDisposable))
                .WithLeadingElasticLineFeed();

            ExpressionSyntax Create(SyntaxToken identifier)
            {
                return semanticModel.UnderscoreFields()
                    ? (ExpressionSyntax)SyntaxFactory.IdentifierName(identifier)
                    : SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ThisExpression(),
                        SyntaxFactory.IdentifierName(identifier));
            }

            bool TryGetMemberAccessFromUsage(SyntaxNode containingNode, out ExpressionSyntax expression)
            {
                using (var identifierNameWalker = IdentifierNameWalker.Borrow(containingNode))
                {
                    foreach (var name in identifierNameWalker.IdentifierNames)
                    {
                        if (name.Identifier.ValueText == disposable.Name &&
                            semanticModel.TryGetSymbol(name, cancellationToken, out var symbol) &&
                            symbol.Equals(disposable.Symbol))
                        {
                            switch (name)
                            {
                                case { Parent: MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _ } memberAccess }:
                                    expression = memberAccess;
                                    return true;
                                case { Parent: ArgumentSyntax _ }:
                                case { Parent: ExpressionSyntax _ }:
                                    expression = name;
                                    return true;
                            }
                        }
                    }
                }

                expression = null;
                return false;
            }

            ExpressionSyntax MemberAccess()
            {
                if (semanticModel.SyntaxTree.TryGetRoot(out var root) &&
                    TryGetMemberAccessFromUsage(root, out var member))
                {
                    return member;
                }

                return Create(
                    SyntaxFacts.GetKeywordKind(disposable.Name) != SyntaxKind.None
                        ? SyntaxFactory.VerbatimIdentifier(default, $"@{disposable.Name}", disposable.Name, default)
                        : SyntaxFactory.Identifier(disposable.Name));
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
