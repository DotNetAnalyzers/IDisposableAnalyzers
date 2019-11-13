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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateAndAssignFieldFix))]
    [Shared]
    internal class CreateAndAssignFieldFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP001DisposeCreated.Descriptor.Id,
            IDISP004DontIgnoreCreated.Descriptor.Id);

        protected override DocumentEditorFixAllProvider FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                      .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == IDISP001DisposeCreated.Descriptor.Id &&
                    node.TryFirstAncestorOrSelf<LocalDeclarationStatementSyntax>(out var localDeclaration) &&
                    localDeclaration is { Declaration: { Type: { } type, Variables: { Count: 1 } variables }, Parent: BlockSyntax { Parent: ConstructorDeclarationSyntax _ } } &&
                    variables[0].Initializer is { } &&
                    semanticModel.TryGetType(type, context.CancellationToken, out var local))
                {
                    context.RegisterCodeFix(
                        "Create and assign field.",
                        (editor, cancellationToken) => CreateAndAssignField(editor, localDeclaration, local),
                        "Create and assign field.",
                        diagnostic);
                }
                else if (diagnostic.Id == IDISP004DontIgnoreCreated.Descriptor.Id &&
                         node.TryFirstAncestorOrSelf<ExpressionStatementSyntax>(out var statement) &&
                         statement.TryFirstAncestor<ConstructorDeclarationSyntax>(out _))
                {
                    context.RegisterCodeFix(
                        "Create and assign field.",
                        (editor, cancellationToken) => CreateAndAssignField(editor, statement),
                        "Create and assign field.",
                        diagnostic);
                }
            }
        }

        private static void CreateAndAssignField(DocumentEditor editor, LocalDeclarationStatementSyntax statement, ITypeSymbol type)
        {
            var containingType = statement.FirstAncestor<TypeDeclarationSyntax>();
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
            var variableDeclarator = statement.Declaration.Variables[0];
            var field = editor.AddField(
                containingType,
                usesUnderscoreNames
                    ? "_" + variableDeclarator.Identifier.ValueText
                    : variableDeclarator.Identifier.ValueText,
                Accessibility.Private,
                DeclarationModifiers.ReadOnly,
                type,
                CancellationToken.None);

            var fieldAccess = usesUnderscoreNames
                ? SyntaxFactory.IdentifierName(field.Declaration.Variables[0].Identifier.ValueText)
                : SyntaxFactory.ParseExpression($"this.{field.Declaration.Variables[0].Identifier.ValueText}");

            editor.ReplaceNode(
                statement,
                (x, g) => g.ExpressionStatement(
                               g.AssignmentStatement(fieldAccess, variableDeclarator.Initializer.Value))
                           .WithTriviaFrom(x));
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
                statement.Expression,
                (x, g) => g.AssignmentStatement(fieldAccess, x).WithTriviaFrom(x));
        }
    }
}
