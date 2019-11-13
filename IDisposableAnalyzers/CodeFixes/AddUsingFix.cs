namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUsingFix))]
    [Shared]
    internal class AddUsingFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.IDISP001DisposeCreated.Id,
            Descriptors.IDISP004DoNotIgnoreCreated.Id,
            Descriptors.IDISP017PreferUsing.Id);

        protected override DocumentEditorFixAllProvider FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == Descriptors.IDISP001DisposeCreated.Id)
                {
                    if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ArgumentSyntax argument) &&
                             argument is { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax { Parent: IfStatementSyntax { Statement: BlockSyntax ifBlock } } } })
                    {
                        if (argument is { Expression: DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax { Identifier: { } identifier } } })
                        {
                            context.RegisterCodeFix(
                                "Add using to end of block.",
                                (editor, _) => editor.ReplaceNode(
                                    ifBlock,
                                    x => SyntaxFactory.Block(
                                        SyntaxFactory.UsingStatement(
                                            null,
                                            SyntaxFactory.IdentifierName(identifier),
                                            x.WithAdditionalAnnotations(Formatter.Annotation)))),
                                "Add using to end of block.",
                                diagnostic);
                        }
                        else if (argument is { Expression: { } expression })
                        {
                            context.RegisterCodeFix(
                                "Add using to end of block.",
                                (editor, _) => editor.ReplaceNode(
                                    ifBlock,
                                    x => SyntaxFactory.Block(
                                        SyntaxFactory.UsingStatement(
                                            null,
                                            expression,
                                            x.WithAdditionalAnnotations(Formatter.Annotation)))),
                                "Add using to end of block.",
                                diagnostic);
                        }
                    }
                    else if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out StatementSyntax statement))
                    {
                        switch (statement)
                        {
                            case LocalDeclarationStatementSyntax { Declaration: { }, Parent: BlockSyntax _ } localDeclarationStatement:
                                context.RegisterCodeFix(
                                    "Add using to end of block.",
                                    (editor, _) => AddUsingToEndOfBlock(editor, localDeclarationStatement),
                                    "Add using to end of block.",
                                    diagnostic);
                                break;
                            case LocalDeclarationStatementSyntax { Declaration: { }, Parent: SwitchSectionSyntax _ } localDeclarationStatement:
                                context.RegisterCodeFix(
                                    "Add using to end of block.",
                                    (editor, _) => AddUsingToEndOfBlock(editor, localDeclarationStatement),
                                    "Add using to end of block.",
                                    diagnostic);
                                break;
                            case ExpressionStatementSyntax { Parent: BlockSyntax _ } expressionStatement:
                                context.RegisterCodeFix(
                                    "Add using to end of block.",
                                    (editor, _) => AddUsingToEndOfBlock(editor, expressionStatement),
                                    "Add using to end of block.",
                                    diagnostic);
                                break;
                        }
                    }
                }
                else if (diagnostic.Id == Descriptors.IDISP004DoNotIgnoreCreated.Id &&
                         syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ExpressionStatementSyntax statement) &&
                         statement.Parent is BlockSyntax)
                {
                    context.RegisterCodeFix(
                        "Add using to end of block.",
                        (editor, _) => AddUsingToEndOfBlock(editor, statement),
                        "Add using to end of block.",
                        diagnostic);
                }
                else if (diagnostic.Id == Descriptors.IDISP017PreferUsing.Id &&
                         syntaxRoot.TryFindNode(diagnostic, out InvocationExpressionSyntax invocation))
                {
                    context.RegisterCodeFix(
                        "Replace with using.",
                        (editor, cancellationToken) => ReplaceWithUsing(editor, invocation, cancellationToken),
                        "Replace with using.",
                        diagnostic);
                }
            }
        }

        private static IReadOnlyList<StatementSyntax> StatementsAfter(StatementSyntax statement)
        {
            return statement switch
            {
                { Parent: BlockSyntax { Statements: { } statements } } => statements
                                                                          .Where(s => s.SpanStart > statement.SpanStart)
                                                                          .Where(x => !x.IsKind(SyntaxKind.LocalFunctionStatement))
                                                                          .ToArray(),
                { Parent: SwitchSectionSyntax { Statements: { } statements } } => statements
                                                                                  .Where(s => s.SpanStart > statement.SpanStart)
                                                                                  .Where(x => !x.IsKind(SyntaxKind.LocalFunctionStatement))
                                                                                  .TakeWhile(x => !x.IsKind(SyntaxKind.BreakStatement))
                                                                                  .ToArray(),
                _ => throw new InvalidOperationException("Statement is not in a block."),
            };
        }

        private static void RemoveStatements(DocumentEditor editor, IEnumerable<StatementSyntax> statements)
        {
            foreach (var statement in statements)
            {
                editor.RemoveNode(statement);
            }
        }

        private static void AddUsingToEndOfBlock(DocumentEditor editor, LocalDeclarationStatementSyntax statement)
        {
            var statements = StatementsAfter(statement);
            RemoveStatements(editor, statements);
            editor.ReplaceNode(
                statement,
                SyntaxFactory.UsingStatement(
                                 declaration: statement.Declaration.WithoutLeadingTrivia(),
                                 expression: null,
                                 statement: SyntaxFactory.Block(SyntaxFactory.List(statements))
                                                         .WithAdditionalAnnotations(Formatter.Annotation))
                             .WithLeadingTriviaFrom(statement.Declaration));
        }

        private static void AddUsingToEndOfBlock(DocumentEditor editor, ExpressionStatementSyntax statement)
        {
            var statements = StatementsAfter(statement);
            RemoveStatements(editor, statements);

            editor.ReplaceNode(
                statement,
                SyntaxFactory.UsingStatement(
                    declaration: null,
                    expression: statement.Expression.WithoutLeadingTrivia(),
                    statement: SyntaxFactory.Block(SyntaxFactory.List(statements))
                                            .WithAdditionalAnnotations(Formatter.Annotation))
                             .WithLeadingTriviaFrom(statement.Expression));
        }

        private static void ReplaceWithUsing(DocumentEditor editor, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            if (DisposeCall.TryGetDisposedRootMember(invocation, editor.SemanticModel, cancellationToken, out var root) &&
                editor.SemanticModel.TryGetSymbol(root, cancellationToken, out ILocalSymbol local) &&
                local.TrySingleDeclaration(cancellationToken, out VariableDeclarationSyntax declaration) &&
                invocation.TryFirstAncestor(out ExpressionStatementSyntax expressionStatement) &&
                declaration.Parent is LocalDeclarationStatementSyntax localDeclarationStatement)
            {
                if (expressionStatement.Parent is BlockSyntax { Parent: FinallyClauseSyntax { Parent: TryStatementSyntax { Block: { } tryBlock } tryStatement } } &&
                    !tryStatement.Catches.Any())
                {
                    if (declaration.Variables.TrySingle(out var declarator) &&
                        declarator.Initializer?.Value.IsKind(SyntaxKind.NullLiteralExpression) == true &&
                        tryBlock.Statements.TryFirst(out var statement) &&
                        statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment })
                    {
                        editor.ReplaceNode(
                            tryStatement,
                            SyntaxFactory.UsingStatement(
                                SyntaxFactory.VariableDeclaration(
                                    SyntaxFactory.ParseTypeName("var"),
                                    SyntaxFactory.SingletonSeparatedList(
                                        declarator.WithInitializer(SyntaxFactory.EqualsValueClause(assignment.Right)))),
                                null,
                                tryStatement.Block.WithStatements(tryStatement.Block.Statements.RemoveAt(0))));
                        editor.RemoveNode(localDeclarationStatement);
                    }
                    else
                    {
                        editor.ReplaceNode(
                            tryStatement,
                            SyntaxFactory.UsingStatement(
                                declaration.WithoutTrailingTrivia(),
                                null,
                                tryBlock));
                        editor.RemoveNode(localDeclarationStatement);
                    }
                }
                else if (localDeclarationStatement.Parent is BlockSyntax block &&
                         block == expressionStatement.Parent)
                {
                    var statements = new List<StatementSyntax>();
                    foreach (var statement in block.Statements.SkipWhile(x => x != localDeclarationStatement).Skip(1))
                    {
                        editor.RemoveNode(statement);
                        if (statement != expressionStatement)
                        {
                            statements.Add(statement);
                        }
                        else
                        {
                            break;
                        }
                    }

                    editor.ReplaceNode(
                        localDeclarationStatement,
                        SyntaxFactory.UsingStatement(
                            declaration.WithoutTrailingTrivia(),
                            null,
                            SyntaxFactory.Block(statements).WithAdditionalAnnotations(Formatter.Annotation)));
                }
            }
        }
    }
}
