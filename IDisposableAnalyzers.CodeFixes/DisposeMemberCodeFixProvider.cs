namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
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
        public override FixAllProvider GetFixAllProvider() => DocumentEditorFixAllProvider.Default;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var member = (MemberDeclarationSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (TryGetMemberSymbol(member, semanticModel, context.CancellationToken, out var memberSymbol))
                {
                    if (Disposable.TryGetDisposeMethod(memberSymbol.ContainingType, ReturnValueSearch.TopLevel, out var disposeMethodSymbol) &&
                        disposeMethodSymbol.TrySingleDeclaration(context.CancellationToken, out MethodDeclarationSyntax disposeMethodDeclaration))
                    {
                        if (disposeMethodSymbol.DeclaredAccessibility == Accessibility.Public &&
                            disposeMethodSymbol.ContainingType == memberSymbol.ContainingType &&
                            disposeMethodSymbol.Parameters.Length == 0)
                        {
                            context.RegisterDocumentEditorFix(
                                "Dispose member.",
                                (editor, cancellationToken) => DisposeInDisposeMethod(editor, memberSymbol, disposeMethodDeclaration, cancellationToken),
                                diagnostic);
                        }

                        if (disposeMethodSymbol.Parameters.Length == 1 &&
                            disposeMethodSymbol.Parameters[0].Type == KnownSymbol.Boolean &&
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
            foreach (var statement in disposeMethod.Body.Statements)
            {
                var ifStatement = statement as IfStatementSyntax;
                if (ifStatement == null)
                {
                    continue;
                }

                if ((ifStatement.Condition as IdentifierNameSyntax)?.Identifier.ValueText == "disposing")
                {
                    result = ifStatement;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static bool TryGetMemberSymbol(MemberDeclarationSyntax member, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol symbol)
        {
            if (member is FieldDeclarationSyntax field &&
                field.Declaration.Variables.TrySingle(out var declarator))
            {
                symbol = semanticModel.GetDeclaredSymbolSafe(declarator, cancellationToken);
                return symbol != null;
            }

            if (member is PropertyDeclarationSyntax property)
            {
                symbol = semanticModel.GetDeclaredSymbolSafe(property, cancellationToken);
                return symbol != null;
            }

            symbol = null;
            return false;
        }
    }
}
