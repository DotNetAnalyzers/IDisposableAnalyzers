namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Simplification;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateAndAssignFieldCodeFixProvider))]
    [Shared]
    internal class CreateAndAssignFieldCodeFixProvider : CodeFixProvider
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
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == IDISP001DisposeCreated.DiagnosticId)
                {
                    var statement = node.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
                    if (statement?.FirstAncestor<ConstructorDeclarationSyntax>() != null &&
                        statement.Declaration.Variables.Count == 1 &&
                        statement.Declaration.Variables[0].Initializer != null)
                    {
                        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                                  .ConfigureAwait(false);
                        if (semanticModel.GetSymbolInfo(statement.Declaration.Type).Symbol is ITypeSymbol type)
                        {
                            context.RegisterDocumentEditorFix(
                                "Create and assign field.",
                                (editor, cancellationToken) => CreateAndAssignField(editor, statement, type, cancellationToken),
                                diagnostic);
                        }
                    }
                }

                if (diagnostic.Id == IDISP004DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId)
                {
                    var statement = node.FirstAncestorOrSelf<ExpressionStatementSyntax>();
                    if (statement?.FirstAncestor<ConstructorDeclarationSyntax>() != null)
                    {
                        context.RegisterDocumentEditorFix(
                            "Create and assign field.",
                            (editor, cancellationToken) => CreateAndAssignField(editor, statement, cancellationToken),
                            diagnostic);
                    }
                }
            }
        }

        private static void CreateAndAssignField(DocumentEditor editor, LocalDeclarationStatementSyntax statement, ITypeSymbol type, CancellationToken cancellationToken)
        {
            var containingType = statement.FirstAncestor<TypeDeclarationSyntax>();
            var usesUnderscoreNames = containingType.UsesUnderscore(editor.SemanticModel, cancellationToken);
            var variableDeclarator = statement.Declaration.Variables[0];
            var identifier = variableDeclarator.Identifier;
            var field = editor.AddField(
                containingType,
                usesUnderscoreNames
                    ? "_" + identifier.ValueText
                    : identifier.ValueText,
                Accessibility.Private,
                DeclarationModifiers.ReadOnly,
                type,
                CancellationToken.None);

            var fieldAccess = usesUnderscoreNames
                ? SyntaxFactory.IdentifierName(field.Name())
                : SyntaxFactory.ParseExpression($"this.{field.Name()}");
            editor.ReplaceNode(
                statement,
                SyntaxFactory.ExpressionStatement(
                                 (ExpressionSyntax)editor.Generator.AssignmentStatement(
                                     fieldAccess,
                                     variableDeclarator.Initializer.Value))
                             .WithLeadingTrivia(statement.GetLeadingTrivia())
                             .WithTrailingTrivia(statement.GetTrailingTrivia()));
        }

        private static void CreateAndAssignField(DocumentEditor editor, ExpressionStatementSyntax statement, CancellationToken cancellationToken)
        {
            var usesUnderscoreNames = editor.SemanticModel.UsesUnderscore(cancellationToken);
            var containingType = statement.FirstAncestor<TypeDeclarationSyntax>();

            var field = editor.AddField(
                containingType,
                usesUnderscoreNames
                    ? "_disposable"
                    : "disposable",
                Accessibility.Private,
                DeclarationModifiers.ReadOnly,
                SyntaxFactory.ParseTypeName("System.IDisposable").WithAdditionalAnnotations(Simplifier.Annotation),
                CancellationToken.None);

            var fieldAccess = usesUnderscoreNames
                ? SyntaxFactory.IdentifierName(field.Name())
                : SyntaxFactory.ParseExpression($"this.{field.Name()}");
            editor.ReplaceNode(
                statement,
                SyntaxFactory.ExpressionStatement(
                                 (ExpressionSyntax)editor.Generator.AssignmentStatement(
                                     fieldAccess,
                                     statement.Expression))
                             .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                             .WithTrailingTrivia(SyntaxFactory.ElasticMarker));
        }
    }
}
