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
            Descriptors.IDISP001DisposeCreated.Id,
            Descriptors.IDISP004DoNotIgnoreCreated.Id);

        protected override DocumentEditorFixAllProvider FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == Descriptors.IDISP001DisposeCreated.Id &&
                    node.TryFirstAncestorOrSelf<LocalDeclarationStatementSyntax>(out var localDeclaration) &&
                    localDeclaration is { Declaration: { Type: { }, Variables: { Count: 1 } variables }, Parent: BlockSyntax { Parent: ConstructorDeclarationSyntax _ } } &&
                    variables[0].Initializer is { })
                {
                    context.RegisterCodeFix(
                        "Create and assign field.",
                        (editor, cancellationToken) => CreateAndAssignField(editor, localDeclaration, cancellationToken),
                        "Create and assign field.",
                        diagnostic);
                }
                else if (diagnostic.Id == Descriptors.IDISP004DoNotIgnoreCreated.Id &&
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

        private static void CreateAndAssignField(DocumentEditor editor, LocalDeclarationStatementSyntax statement, CancellationToken cancellationToken)
        {
            var local = statement.Declaration.Variables[0];
            var fieldAccess = AddField(
                editor,
                statement.FirstAncestor<TypeDeclarationSyntax>(),
                local.Identifier.ValueText,
                Accessibility.Private,
                DeclarationModifiers.ReadOnly,
                editor.SemanticModel.GetTypeInfoSafe(statement.Declaration.Type, cancellationToken).Type,
                cancellationToken);

            editor.ReplaceNode(
                statement,
                (x, g) => g.ExpressionStatement(
                               g.AssignmentStatement(fieldAccess, local.Initializer.Value))
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

        private static ExpressionSyntax AddField(DocumentEditor editor, TypeDeclarationSyntax containingType, string name, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol type, CancellationToken cancellationToken)
        {
            var usesUnderscoreNames = editor.SemanticModel.UnderscoreFields();
            if (usesUnderscoreNames)
            {
                name = $"_{name}";
            }

            var field = editor.AddField(
                containingType,
                name,
                accessibility,
                modifiers,
                (TypeSyntax)editor.Generator.TypeExpression(type),
                cancellationToken);
            var identifierNameSyntax = SyntaxFactory.IdentifierName(field.Declaration.Variables[0].Identifier);
            return usesUnderscoreNames
                ? (ExpressionSyntax)identifierNameSyntax
                : SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    identifierNameSyntax);
        }
    }
}
