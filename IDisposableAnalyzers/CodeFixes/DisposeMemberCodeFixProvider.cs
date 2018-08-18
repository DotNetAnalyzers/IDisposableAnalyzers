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
                if (syntaxRoot.TryFindNode<MemberDeclarationSyntax>(diagnostic, out var member) &&
                    semanticModel.TryGetSymbol(member, context.CancellationToken, out ISymbol memberSymbol))
                {
                    if (DisposeMethod.TryFindVirtualDispose(memberSymbol.ContainingType, semanticModel.Compilation, Search.TopLevel, out var disposeMethod) &&
                        disposeMethod.TrySingleDeclaration(context.CancellationToken, out MethodDeclarationSyntax disposeMethodDeclaration))
                    {
                        context.RegisterDocumentEditorFix(
                            "Dispose member.",
                            (editor, cancellationToken) => DisposeInVirtualDisposeMethod(editor, memberSymbol, disposeMethodDeclaration, cancellationToken),
                            diagnostic);
                    }
                    else if (DisposeMethod.TryFindIDisposableDispose(memberSymbol.ContainingType, semanticModel.Compilation, Search.TopLevel, out disposeMethod) &&
                        disposeMethod.TrySingleDeclaration(context.CancellationToken, out disposeMethodDeclaration))
                    {
                        context.RegisterDocumentEditorFix(
                            "Dispose member.",
                            (editor, cancellationToken) => DisposeInDisposeMethod(editor, memberSymbol, disposeMethodDeclaration, cancellationToken),
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

        private static void DisposeInVirtualDisposeMethod(DocumentEditor editor, ISymbol memberSymbol, MethodDeclarationSyntax disposeMethodDeclaration, CancellationToken cancellationToken)
        {
            var disposeStatement = Snippet.DisposeStatement(memberSymbol, editor.SemanticModel, cancellationToken);
            if (TryFindIfDisposing(disposeMethodDeclaration, out var ifDisposing))
            {
                if (ifDisposing.Statement is BlockSyntax block)
                {
                    var statements = block.Statements.Add(disposeStatement);
                    var newBlock = block.WithStatements(statements);
                    editor.ReplaceNode(block, newBlock);
                }
                else if (ifDisposing.Statement is StatementSyntax statement)
                {
                    editor.ReplaceNode(
                        ifDisposing,
                        ifDisposing.WithStatement(SyntaxFactory.Block(statement, disposeStatement)));
                }
                else
                {
                    editor.ReplaceNode(
                        ifDisposing,
                        ifDisposing.WithStatement(SyntaxFactory.Block(disposeStatement)));
                }
            }
            else if (disposeMethodDeclaration.Body is BlockSyntax block)
            {
                ifDisposing = SyntaxFactory.IfStatement(
                    SyntaxFactory.IdentifierName(disposeMethodDeclaration.ParameterList.Parameters[0].Identifier),
                    SyntaxFactory.Block(disposeStatement));

                if (DisposeMethod.TryGetBaseCall(disposeMethodDeclaration, editor.SemanticModel, cancellationToken, out var baseCall))
                {
                    if (baseCall.TryFirstAncestor(out ExpressionStatementSyntax expressionStatement))
                    {
                        editor.InsertBefore(expressionStatement, ifDisposing);
                    }
                }
                else
                {
                    editor.ReplaceNode(
                        block,
                        block.AddStatements(ifDisposing));
                }
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

        private static bool TryFindIfDisposing(MethodDeclarationSyntax disposeMethod, out IfStatementSyntax result)
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
