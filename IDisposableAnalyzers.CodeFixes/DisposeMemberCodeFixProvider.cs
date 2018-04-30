namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeMemberCodeFixProvider))]
    [Shared]
    internal class DisposeMemberCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP002DisposeMember.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => DocumentEditorFixAllProvider.Document;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan) is MemberDeclarationSyntax member &&
                    semanticModel.TryGetSymbol(member, context.CancellationToken, out ISymbol memberSymbol) &&
                    Disposable.TryGetDisposeMethod(memberSymbol.ContainingType, semanticModel.Compilation, Search.TopLevel, out var disposeMethod) &&
                    disposeMethod.TrySingleDeclaration(context.CancellationToken, out MethodDeclarationSyntax disposeMethodDeclaration))
                {
                    if (disposeMethod.DeclaredAccessibility == Accessibility.Public &&
                        disposeMethod.ContainingType == memberSymbol.ContainingType &&
                        disposeMethod.Parameters.Length == 0)
                    {
                        context.RegisterDocumentEditorFix(
                            "Dispose member.",
                            (editor, cancellationToken) => DisposeInDisposeMethod(editor, memberSymbol, disposeMethodDeclaration, cancellationToken),
                            diagnostic);
                    }

                    if (disposeMethod.Parameters.TrySingle(out var parameter) &&
                        parameter.Type == KnownSymbol.Boolean &&
                        TryGetIfDisposing(disposeMethodDeclaration, out var ifDisposing))
                    {
                        context.RegisterDocumentEditorFix(
                            "Dispose member.",
                            (editor, cancellationToken) => DisposeInVirtualDisposeMethod(editor, memberSymbol, ifDisposing, cancellationToken),
                            diagnostic);
                    }
                }
            }
        }

        private static void DisposeInDisposeMethod(DocumentEditor editor, ISymbol memberSymbol, MethodDeclarationSyntax disposeMethod, CancellationToken cancellationToken)
        {
            var disposeStatement = Snippet.DisposeStatement(memberSymbol, editor.SemanticModel, cancellationToken);
            var statements = CreateStatements(disposeMethod, disposeStatement);
            if (disposeMethod.Body != null)
            {
                var updatedBody = disposeMethod.Body.WithStatements(statements);
                editor.ReplaceNode(disposeMethod.Body, updatedBody);
            }
            else if (disposeMethod.ExpressionBody != null)
            {
                var newMethod = disposeMethod.WithBody(SyntaxFactory.Block(statements))
                                             .WithExpressionBody(null)
                                             .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));
                editor.ReplaceNode(disposeMethod, newMethod);
            }
        }

        private static void DisposeInVirtualDisposeMethod(DocumentEditor editor, ISymbol memberSymbol, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var disposeStatement = Snippet.DisposeStatement(memberSymbol, editor.SemanticModel, cancellationToken);
            if (ifStatement.Statement is BlockSyntax block)
            {
                var statements = block.Statements.Add(disposeStatement);
                var newBlock = block.WithStatements(statements);
                editor.ReplaceNode(block, newBlock);
            }
            else if (ifStatement.Statement is StatementSyntax statement)
            {
                editor.ReplaceNode(
                    ifStatement,
                    ifStatement.WithStatement(SyntaxFactory.Block(statement, disposeStatement)));
            }
            else
            {
                editor.ReplaceNode(
                    ifStatement,
                    ifStatement.WithStatement(SyntaxFactory.Block(disposeStatement)));
            }
        }

        private static SyntaxList<StatementSyntax> CreateStatements(MethodDeclarationSyntax method, StatementSyntax newStatement)
        {
            if (method.ExpressionBody != null)
            {
                return SyntaxFactory.List(new[] { SyntaxFactory.ExpressionStatement(method.ExpressionBody.Expression), newStatement });
            }

            return method.Body.Statements.Add(newStatement);
        }

        private static bool TryGetIfDisposing(MethodDeclarationSyntax disposeMethod, out IfStatementSyntax result)
        {
            if (disposeMethod.ParameterList is ParameterListSyntax parameterList &&
                parameterList.Parameters.TrySingle(out var parameter) &&
                parameter.Type == KnownSymbol.Boolean)
            {
                foreach (var statement in disposeMethod.Body.Statements)
                {
                    if (statement is IfStatementSyntax ifStatement &&
                        ifStatement.Condition is IdentifierNameSyntax identifierName &&
                        identifierName.Identifier.ValueText == parameter.Identifier.ValueText)
                    {
                        result = ifStatement;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }
    }
}
