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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CreateAndAssignFieldFix))]
    [Shared]
    internal class CreateAndAssignFieldFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.IDISP001DisposeCreated.Id,
            Descriptors.IDISP004DoNotIgnoreCreated.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == Descriptors.IDISP001DisposeCreated.Id &&
                    syntaxRoot?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<LocalDeclarationStatementSyntax>() is { } localDeclaration &&
                    localDeclaration is { Declaration: { Type: { } type, Variables: { Count: 1 } variables }, Parent: BlockSyntax { Parent: ConstructorDeclarationSyntax _ } } &&
                    variables[0] is { Initializer: { } } local &&
                    localDeclaration.TryFirstAncestor(out TypeDeclarationSyntax? containingType))
                {
                    context.RegisterCodeFix(
                        "Create and assign field.",
                        (editor, cancellationToken) => CreateAndAssignFieldAsync(editor, cancellationToken),
                        "Create and assign field.",
                        diagnostic);

                    async Task CreateAndAssignFieldAsync(DocumentEditor editor, CancellationToken cancellationToken)
                    {
                        var fieldAccess = await editor.AddFieldAsync(
                                                          containingType!,
                                                          local!.Identifier.ValueText,
                                                          Accessibility.Private,
                                                          DeclarationModifiers.ReadOnly,
                                                          editor.SemanticModel.GetType(type!, cancellationToken)!,
                                                          cancellationToken)
                                                      .ConfigureAwait(false);

                        editor.ReplaceNode(
                            localDeclaration,
                            (x, g) => g.ExpressionStatement(
                                           g.AssignmentStatement(fieldAccess, local.Initializer!.Value))
                                       .WithTriviaFrom(x));
                    }
                }
                else if (diagnostic.Id == Descriptors.IDISP004DoNotIgnoreCreated.Id &&
                         syntaxRoot?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ExpressionStatementSyntax>() is { } statement &&
                         statement.TryFirstAncestor<ConstructorDeclarationSyntax>(out var ctor) &&
                         ctor is { Parent: TypeDeclarationSyntax parent })
                {
                    context.RegisterCodeFix(
                        "Create and assign field.",
                        (editor, cancellationToken) => CreateAndAssignFieldAsync(editor, cancellationToken),
                        "Create and assign field.",
                        diagnostic);

                    async Task CreateAndAssignFieldAsync(DocumentEditor editor, CancellationToken cancellationToken)
                    {
                        var fieldAccess = await editor.AddFieldAsync(
                                                          parent,
                                                          "disposable",
                                                          Accessibility.Private,
                                                          DeclarationModifiers.ReadOnly,
                                                          IDisposableFactory.SystemIDisposable,
                                                          cancellationToken)
                                                      .ConfigureAwait(false);

                        _ = editor.ReplaceNode(
                            statement!,
                            x => SyntaxFactory.ExpressionStatement(
                                           SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, fieldAccess, x.Expression))
                                       .WithTriviaFrom(x));
                    }
                }
            }
        }
    }
}
