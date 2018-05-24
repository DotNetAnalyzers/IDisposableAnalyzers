namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUsingCodeFixProvider))]
    [Shared]
    internal class AddUsingCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP001DisposeCreated.DiagnosticId,
            IDISP004DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => null;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == IDISP001DisposeCreated.DiagnosticId)
                {
                    if (node.TryFirstAncestorOrSelf<LocalDeclarationStatementSyntax>(out var statement))
                    {
                        switch (statement.Parent)
                        {
                            case BlockSyntax block:
                                context.RegisterDocumentEditorFix(
                                    "Add using to end of block.",
                                    (editor, _) => AddUsing(editor, block, statement),
                                    diagnostic);
                                break;
                            case SwitchSectionSyntax switchSection:
                                context.RegisterDocumentEditorFix(
                                    "Add using to end of block.",
                                    (editor, _) => AddUsing(editor, switchSection, statement),
                                    diagnostic);
                                break;
                        }
                    }
                    else if (node.TryFirstAncestorOrSelf<ExpressionStatementSyntax>(out var expressionStatement) &&
                             expressionStatement.Parent is BlockSyntax expressionStatementBlock)
                    {
                        context.RegisterDocumentEditorFix(
                            "Add using to end of block.",
                            (editor, _) => AddUsing(editor, expressionStatementBlock, expressionStatement),
                            diagnostic);
                    }
                    else if (node is ArgumentSyntax argument &&
                            argument.Parent is ArgumentListSyntax argumentList &&
                            argumentList.Parent is InvocationExpressionSyntax invocation &&
                            invocation.Parent is IfStatementSyntax ifStatement &&
                            ifStatement.Statement is BlockSyntax ifBlock)
                    {
                        context.RegisterDocumentEditorFix(
                            "Add using to end of block.",
                            (editor, _) => AddUsing(editor, ifBlock, argument.Expression),
                            diagnostic);
                    }
                }

                if (diagnostic.Id == IDISP004DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId)
                {
                    if (node.TryFirstAncestorOrSelf<ExpressionStatementSyntax>(out var statement) &&
                        statement.Parent is BlockSyntax block)
                    {
                        context.RegisterDocumentEditorFix(
                            "Add using to end of block.",
                            (editor, _) => AddUsing(editor, block, statement),
                            diagnostic);
                    }
                }
            }
        }

        private static void AddUsing(DocumentEditor editor, BlockSyntax block, LocalDeclarationStatementSyntax statement)
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
                    declaration: statement.Declaration,
                    expression: null,
                    statement: SyntaxFactory.Block(SyntaxFactory.List(statements))
                                            .WithAdditionalAnnotations(Formatter.Annotation)));
        }

        private static void AddUsing(DocumentEditor editor, SwitchSectionSyntax switchSection, LocalDeclarationStatementSyntax statement)
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

        private static void AddUsing(DocumentEditor editor, BlockSyntax block, ExpressionStatementSyntax statement)
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

        private static void AddUsing(DocumentEditor editor, BlockSyntax block, ExpressionSyntax expression)
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
    }
}
