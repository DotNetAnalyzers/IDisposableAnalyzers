namespace IDisposableAnalyzers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;

    // ReSharper disable once InconsistentNaming
    internal static class IDisposableFactory
    {
        internal static readonly TypeSyntax SystemIDisposable =
            SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("IDisposable"))
                         .WithAdditionalAnnotations(Simplifier.Annotation);

        internal static readonly StatementSyntax GcSuppressFinalizeThis =
            SyntaxFactory.ExpressionStatement(
                             SyntaxFactory.InvocationExpression(
                                 SyntaxFactory.MemberAccessExpression(
                                     SyntaxKind.SimpleMemberAccessExpression,
                                     SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("GC")),
                                     SyntaxFactory.IdentifierName("SuppressFinalize")),
                                 Arguments(SyntaxFactory.ThisExpression())))
                         .WithAdditionalAnnotations(Simplifier.Annotation);

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
                    case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _, Name: { } }:
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
                        DisposeMethod.Find(type, semanticModel.Compilation, Search.Recursive) is { ExplicitInterfaceImplementations: { IsEmpty: true } })
                    {
                        return e.WithoutTrivia()
                                .WithLeadingElasticLineFeed();
                    }

                    return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(SystemIDisposable, e));
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
                        DisposeMethod.Find(disposable.Type, semanticModel.Compilation, Search.Recursive) is { ExplicitInterfaceImplementations: { IsEmpty: true } })
                    {
                        return DisposeStatement(neverNull.WithoutTrivia()).WithLeadingElasticLineFeed();
                    }

                    return DisposeStatement(
                        SyntaxFactory.CastExpression(
                            SystemIDisposable,
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
                        disposable.Initializer(cancellationToken) is { Value: ObjectCreationExpressionSyntax _ })
                    {
                        memberAccess = MemberAccess(disposable, semanticModel, cancellationToken);
                        return true;
                    }

                    memberAccess = null!;
                    return false;
                }
            }

            if (DisposeMethod.IsAccessibleOn(disposable.Type, semanticModel.Compilation))
            {
                return ConditionalDisposeStatement(MemberAccess(disposable, semanticModel, cancellationToken)).WithLeadingElasticLineFeed();
            }

            return ConditionalDisposeStatement(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.AsExpression,
                        MemberAccess(disposable, semanticModel, cancellationToken),
                        SystemIDisposable))
                .WithLeadingElasticLineFeed();
        }

        internal static ExpressionSyntax MemberAccess(SyntaxToken memberIdentifier, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (semanticModel.SyntaxTree.TryGetRoot(out var root) &&
                semanticModel.GetSymbolSafe(memberIdentifier.Parent, cancellationToken) is { } member &&
                FieldOrProperty.TryCreate(member, out var fieldOrProperty) &&
                TryGetMemberAccessFromUsage(root, fieldOrProperty, semanticModel, cancellationToken, out var memberAccess))
            {
                return memberAccess;
            }

            return semanticModel.UnderscoreFields() == CodeStyleResult.Yes
                ? (ExpressionSyntax)SyntaxFactory.IdentifierName(memberIdentifier)
                : SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName(memberIdentifier));
        }

        internal static ExpressionSyntax MemberAccess(FieldOrProperty member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (semanticModel.SyntaxTree.TryGetRoot(out var root) &&
                TryGetMemberAccessFromUsage(root, member, semanticModel, cancellationToken, out var memberAccess))
            {
                return memberAccess;
            }

            return Create(
                SyntaxFacts.GetKeywordKind(member.Name) != SyntaxKind.None
                    ? SyntaxFactory.VerbatimIdentifier(default, $"@{member.Name}", member.Name, default)
                    : SyntaxFactory.Identifier(member.Name));

            ExpressionSyntax Create(SyntaxToken identifier)
            {
                return semanticModel.UnderscoreFields() == CodeStyleResult.Yes
                    ? (ExpressionSyntax)SyntaxFactory.IdentifierName(identifier)
                    : SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ThisExpression(),
                        SyntaxFactory.IdentifierName(identifier));
            }
        }

        internal static bool TryGetMemberAccessFromUsage(SyntaxNode containingNode, FieldOrProperty member, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ExpressionSyntax? expression)
        {
            using (var identifierNameWalker = IdentifierNameWalker.Borrow(containingNode))
            {
                foreach (var name in identifierNameWalker.IdentifierNames)
                {
                    if (name.Identifier.ValueText == member.Name &&
                        semanticModel.TryGetSymbol(name, cancellationToken, out var symbol) &&
                        symbol.Equals(member.Symbol))
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
            return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, e, SystemIDisposable));
        }

        internal static ArgumentListSyntax Arguments(ExpressionSyntax expression)
        {
            return SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(expression)));
        }
    }
}
