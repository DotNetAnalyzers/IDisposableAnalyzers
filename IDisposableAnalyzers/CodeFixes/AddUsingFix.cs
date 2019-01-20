namespace IDisposableAnalyzers
{
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
    internal class AddUsingFix : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP001DisposeCreated.DiagnosticId,
            IDISP004DontIgnoreCreated.DiagnosticId,
            IDISP017PreferUsing.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => null;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == IDISP001DisposeCreated.DiagnosticId)
                {
                    if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out LocalDeclarationStatementSyntax statement))
                    {
                        switch (statement.Parent)
                        {
                            case BlockSyntax block:
                                context.RegisterDocumentEditorFix(
                                    "Add using to end of block.",
                                    (editor, _) => AddUsingToEndOfBlock(editor, block, statement),
                                    diagnostic);
                                break;
                            case SwitchSectionSyntax switchSection:
                                context.RegisterDocumentEditorFix(
                                    "Add using to end of block.",
                                    (editor, _) => AddUsingToEndOfBlock(editor, switchSection, statement),
                                    diagnostic);
                                break;
                        }
                    }
                    else if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ExpressionStatementSyntax expressionStatement) &&
                             expressionStatement.Parent is BlockSyntax expressionStatementBlock)
                    {
                        context.RegisterDocumentEditorFix(
                            "Add using to end of block.",
                            (editor, _) => AddUsingToEndOfBlock(editor, expressionStatementBlock, expressionStatement),
                            diagnostic);
                    }
                    else if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ArgumentSyntax argument) &&
                            argument.Parent is ArgumentListSyntax argumentList &&
                            argumentList.Parent is InvocationExpressionSyntax invocation &&
                            invocation.Parent is IfStatementSyntax ifStatement &&
                            ifStatement.Statement is BlockSyntax ifBlock)
                    {
                        context.RegisterDocumentEditorFix(
                            "Add using to end of block.",
                            (editor, _) => AddUsingToEndOfBlock(editor, ifBlock, argument.Expression),
                            diagnostic);
                    }
                }
                else if (diagnostic.Id == IDISP004DontIgnoreCreated.DiagnosticId &&
                         syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ExpressionStatementSyntax statement) &&
                         statement.Parent is BlockSyntax block)
                {
                    context.RegisterDocumentEditorFix(
                        "Add using to end of block.",
                        (editor, _) => AddUsingToEndOfBlock(editor, block, statement),
                        diagnostic);
                }
                else if (diagnostic.Id == IDISP017PreferUsing.DiagnosticId &&
                         syntaxRoot.TryFindNode(diagnostic, out InvocationExpressionSyntax invocation))
                {
                    context.RegisterDocumentEditorFix(
                        "Replace with using.",
                        (editor, cancellationToken) => ReplaceWithUsing(editor, invocation, cancellationToken),
                        diagnostic);
                }
            }
        }

        private static void AddUsingToEndOfBlock(DocumentEditor editor, BlockSyntax block, LocalDeclarationStatementSyntax statement)
        {
            var statements = block.Statements
                                  .Where(s => s.SpanStart > statement.SpanStart)
                                  .TakeWhile(s => !(s is LocalFunctionStatementSyntax))
                                  .ToArray();
            foreach (var statementSyntax in statements)
            {
                editor.RemoveNode(statementSyntax);
            }

            editor.ReplaceNode(
                statement,
                SyntaxFactory.UsingStatement(
                                 declaration: statement.Declaration.WithoutLeadingTrivia(),
                                 expression: null,
                                 statement: SyntaxFactory.Block(SyntaxFactory.List(statements))
                                                         .WithAdditionalAnnotations(Formatter.Annotation))
                             .WithLeadingTriviaFrom(statement.Declaration));
        }

        private static void AddUsingToEndOfBlock(DocumentEditor editor, SwitchSectionSyntax switchSection, LocalDeclarationStatementSyntax statement)
        {
            var statements = switchSection.Statements
                                  .Where(s => s.SpanStart > statement.SpanStart)
                                          .Where(s => !(s == switchSection.Statements.Last() &&
                                                        s is BreakStatementSyntax))
                                  .ToArray();
            foreach (var statementSyntax in statements)
            {
                editor.RemoveNode(statementSyntax);
            }

            editor.ReplaceNode(
                statement,
                SyntaxFactory.UsingStatement(
                    declaration: statement.Declaration,
                    expression: null,
                    statement: SyntaxFactory.Block(SyntaxFactory.List(statements))
                                            .WithAdditionalAnnotations(Formatter.Annotation)));
        }

        private static void AddUsingToEndOfBlock(DocumentEditor editor, BlockSyntax block, ExpressionStatementSyntax statement)
        {
            var statements = block.Statements
                                  .Where(s => s.SpanStart > statement.SpanStart)
                                  .ToArray();
            foreach (var statementSyntax in statements)
            {
                editor.RemoveNode(statementSyntax);
            }

            editor.ReplaceNode(
                statement,
                SyntaxFactory.UsingStatement(
                    declaration: null,
                    expression: statement.Expression,
                    statement: SyntaxFactory.Block(SyntaxFactory.List(statements))
                                            .WithAdditionalAnnotations(Formatter.Annotation)));
        }

        private static void AddUsingToEndOfBlock(DocumentEditor editor, BlockSyntax block, ExpressionSyntax expression)
        {
            foreach (var statementSyntax in block.Statements)
            {
                editor.RemoveNode(statementSyntax);
            }

            editor.ReplaceNode(
                block,
                SyntaxFactory.Block(
                    SyntaxFactory.UsingStatement(
                        declaration: null,
                        expression: GetExpression(),
                        statement: block.WithAdditionalAnnotations(Formatter.Annotation))));

            ExpressionSyntax GetExpression()
            {
                if (expression is DeclarationExpressionSyntax declaration &&
                    declaration.Designation is SingleVariableDesignationSyntax singleVariable)
                {
                    return SyntaxFactory.IdentifierName(singleVariable.Identifier);
                }

                return expression;
            }
        }

        private static void ReplaceWithUsing(DocumentEditor editor, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            if (DisposeCall.TryGetDisposedRootMember(invocation, editor.SemanticModel, cancellationToken, out var root) &&
                editor.SemanticModel.TryGetSymbol(root, cancellationToken, out ILocalSymbol local) &&
                local.TrySingleDeclaration(cancellationToken, out VariableDeclarationSyntax declaration) &&
                invocation.TryFirstAncestor(out ExpressionStatementSyntax expressionStatement) &&
                declaration.Parent is LocalDeclarationStatementSyntax localDeclarationStatement)
            {
                if (expressionStatement.Parent is BlockSyntax finallyBlock &&
                    finallyBlock.Parent is FinallyClauseSyntax finallyClause &&
                    finallyClause.Parent is TryStatementSyntax tryStatement &&
                    !tryStatement.Catches.Any())
                {
                    if (declaration.Variables.TrySingle(out var declarator) &&
                        declarator.Initializer?.Value.IsKind(SyntaxKind.NullLiteralExpression) == true &&
                        tryStatement.Block.Statements.TryFirst(out var statement) &&
                        statement is ExpressionStatementSyntax assignExpressionStatement &&
                        assignExpressionStatement.Expression is AssignmentExpressionSyntax assignment)
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
                                tryStatement.Block));
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
