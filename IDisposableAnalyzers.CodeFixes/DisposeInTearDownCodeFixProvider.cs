namespace IDisposableAnalyzers
{
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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeInTearDownCodeFixProvider))]
    [Shared]
    internal class DisposeInTearDownCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP002DisposeMember.DiagnosticId,
            IDISP003DisposeBeforeReassigning.DiagnosticId);

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

                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                var member = node as MemberDeclarationSyntax ??
                             (SyntaxNode)(node as AssignmentExpressionSyntax)?.Left;
                if (TryGetMemberSymbol(member, semanticModel, context.CancellationToken, out var memberSymbol))
                {
                    if (TestFixture.IsAssignedInSetUp(memberSymbol, SyntaxNodeExt.FirstAncestor<ClassDeclarationSyntax>(member), semanticModel, context.CancellationToken, out var setupAttribute))
                    {
                        if (TestFixture.TryGetTearDownMethod(setupAttribute, semanticModel, context.CancellationToken, out var tearDownMethodDeclaration))
                        {
                            context.RegisterDocumentEditorFix(
                                $"Dispose member in {tearDownMethodDeclaration.Identifier.ValueText}.",
                                (editor, cancellationToken) => DisposeInTearDownMethod(editor, memberSymbol, tearDownMethodDeclaration, cancellationToken),
                                diagnostic);
                        }
                        else if (SyntaxNodeExt.FirstAncestor<MethodDeclarationSyntax>(setupAttribute) is MethodDeclarationSyntax setupMethod)
                        {
                            var tearDownType = SemanticModelExt.GetTypeInfoSafe(semanticModel, setupAttribute, context.CancellationToken).Type == KnownSymbol.NUnitSetUpAttribute
                                ? KnownSymbol.NUnitTearDownAttribute
                                : KnownSymbol.NUnitOneTimeTearDownAttribute;

                            context.RegisterDocumentEditorFix(
                                $"Create {tearDownType.Type} method and dispose member.",
                                (editor, cancellationToken) => CreateTearDownMethod(editor, memberSymbol, setupMethod, tearDownType, cancellationToken),
                                diagnostic);
                        }
                    }
                }
            }
        }

        private static void DisposeInTearDownMethod(DocumentEditor editor, ISymbol fieldOrProperty, MethodDeclarationSyntax disposeMethod, CancellationToken cancellationToken)
        {
            var disposeStatement = Snippet.DisposeStatement(fieldOrProperty, editor.SemanticModel, cancellationToken);
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

        private static void CreateTearDownMethod(DocumentEditor editor, ISymbol fieldOrProperty, MethodDeclarationSyntax setupMethod, QualifiedType tearDownType, CancellationToken cancellationToken)
        {
            var code = StringBuilderPool.Borrow()
                                        .AppendLine($"[{tearDownType.FullName}]")
                                        .AppendLine($"public void {tearDownType.Type.Replace("Attribute", string.Empty)}()")
                                        .AppendLine("{")
                                        .AppendLine($"    {Snippet.DisposeStatement(fieldOrProperty, editor.SemanticModel, cancellationToken)}")
                                        .AppendLine("}")
                                        .Return();
            var tearDownMethod = (MethodDeclarationSyntax)SyntaxFactory.ParseCompilationUnit(code)
                                                                       .Members
                                                                       .Single()
                                                                       .WithSimplifiedNames()
                                                                       .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                                                       .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                                                                       .WithAdditionalAnnotations(Formatter.Annotation);
            editor.InsertAfter(setupMethod, tearDownMethod);
        }

        private static SyntaxList<StatementSyntax> CreateStatements(MethodDeclarationSyntax method, StatementSyntax newStatement)
        {
            if (method.ExpressionBody != null)
            {
                return SyntaxFactory.List(new[] { SyntaxFactory.ExpressionStatement(method.ExpressionBody.Expression), newStatement });
            }

            return method.Body.Statements.Add(newStatement);
        }

        private static bool TryGetMemberSymbol(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, out ISymbol symbol)
        {
            symbol = SemanticModelExt.GetSymbolSafe(semanticModel, node, cancellationToken) ??
                     SemanticModelExt.GetDeclaredSymbolSafe(semanticModel, node, cancellationToken);
            return symbol != null && SymbolExt.IsEither<IFieldSymbol, IPropertySymbol>(symbol);
        }
    }
}
