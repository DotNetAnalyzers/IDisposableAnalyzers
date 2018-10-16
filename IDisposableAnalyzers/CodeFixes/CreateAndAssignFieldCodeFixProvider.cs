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
    using Microsoft.CodeAnalysis.Simplification;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateAndAssignFieldCodeFixProvider))]
    [Shared]
    internal class CreateAndAssignFieldCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP001DisposeCreated.DiagnosticId,
            IDISP004DontIgnoreCreated.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => null;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == IDISP001DisposeCreated.DiagnosticId &&
                    node.TryFirstAncestorOrSelf<LocalDeclarationStatementSyntax>(out var localDeclaration) &&
                    localDeclaration.TryFirstAncestor<ConstructorDeclarationSyntax>(out _) &&
                    localDeclaration.Declaration.Variables.TrySingle(out var variable) &&
                    variable.Initializer != null &&
                    semanticModel.TryGetType(localDeclaration.Declaration.Type, context.CancellationToken, out var type))
                {
                    context.RegisterDocumentEditorFix(
                        "Create and assign field.",
                        (editor, cancellationToken) => CreateAndAssignField(editor, localDeclaration, type),
                        diagnostic);
                }
                else if (diagnostic.Id == IDISP004DontIgnoreCreated.DiagnosticId &&
                         node.TryFirstAncestorOrSelf<ExpressionStatementSyntax>(out var statement) &&
                         statement.TryFirstAncestor<ConstructorDeclarationSyntax>(out _))
                {
                    context.RegisterDocumentEditorFix(
                        "Create and assign field.",
                        (editor, cancellationToken) => CreateAndAssignField(editor, statement),
                        diagnostic);
                }
            }
        }

        private static void CreateAndAssignField(DocumentEditor editor, LocalDeclarationStatementSyntax statement, ITypeSymbol type)
        {
            var containingType = statement.FirstAncestor<TypeDeclarationSyntax>();
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
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
                ? SyntaxFactory.IdentifierName(field.Declaration.Variables[0].Identifier.ValueText)
                : SyntaxFactory.ParseExpression($"this.{field.Declaration.Variables[0].Identifier.ValueText}");
            editor.ReplaceNode(
                statement,
                SyntaxFactory.ExpressionStatement(
                                 (ExpressionSyntax)editor.Generator.AssignmentStatement(
                                     fieldAccess,
                                     variableDeclarator.Initializer.Value))
                             .WithLeadingTrivia(statement.GetLeadingTrivia())
                             .WithTrailingTrivia(statement.GetTrailingTrivia()));
        }

        private static void CreateAndAssignField(DocumentEditor editor, ExpressionStatementSyntax statement)
        {
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
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
                ? SyntaxFactory.IdentifierName(field.Declaration.Variables[0].Identifier.ValueText)
                : SyntaxFactory.ParseExpression($"this.{field.Declaration.Variables[0].Identifier.ValueText}");
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
