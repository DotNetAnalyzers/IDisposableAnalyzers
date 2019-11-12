namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeInTearDownFix))]
    [Shared]
    internal class DisposeInTearDownFix : DocumentEditorCodeFixProvider
    {
        private static readonly MethodDeclarationSyntax TearDownMethod = CreateTearDownMethod(KnownSymbol.NUnitTearDownAttribute);
        private static readonly MethodDeclarationSyntax OneTimeTearDownMethod = CreateTearDownMethod(KnownSymbol.NUnitOneTimeTearDownAttribute);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP002DisposeMember.DiagnosticId,
            IDISP003DisposeBeforeReassigning.DiagnosticId);

        protected override DocumentEditorFixAllProvider FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (TryGetMemberAccess(out var member) &&
                    semanticModel.TryGetSymbol(member, context.CancellationToken, out ISymbol memberSymbol) &&
                    FieldOrProperty.TryCreate(memberSymbol, out var fieldOrProperty) &&
                    TestFixture.IsAssignedInSetUp(fieldOrProperty, member.FirstAncestor<ClassDeclarationSyntax>(), semanticModel, context.CancellationToken, out var assignment, out var setupAttribute) &&
                    assignment is { Left: { } left })
                {
                    if (TestFixture.TryGetTearDownMethod(setupAttribute, semanticModel, context.CancellationToken, out var tearDown))
                    {
                        switch (tearDown)
                        {
                            case { Body: { } body }:
                                context.RegisterCodeFix(
                                    $"Dispose member in {tearDown.Identifier.ValueText}.",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        body,
                                        x => x.AddStatements(IDisposableFactory.DisposeStatement(left, editor.SemanticModel, cancellationToken))),
                                    $"Dispose member in {tearDown.Identifier.ValueText}.",
                                    diagnostic);
                                break;
                            case { ExpressionBody: { Expression: { } expression } }:
                                context.RegisterCodeFix(
                                    $"Dispose member in {tearDown.Identifier.ValueText}.",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        tearDown,
                                        x => x.AsBlockBody(
                                            SyntaxFactory.ExpressionStatement(expression),
                                            IDisposableFactory.DisposeStatement(left, editor.SemanticModel, cancellationToken))),
                                    $"Dispose member in {tearDown.Identifier.ValueText}.",
                                    diagnostic);
                                break;
                        }
                    }
                    else if (setupAttribute.TryFirstAncestor<MethodDeclarationSyntax>(out var setupMethod))
                    {
                        tearDown = semanticModel.GetTypeInfoSafe(setupAttribute, context.CancellationToken).Type == KnownSymbol.NUnitSetUpAttribute
                            ? TearDownMethod
                            : OneTimeTearDownMethod;

                        if (setupMethod.Modifiers.Any(SyntaxKind.StaticKeyword))
                        {
                            tearDown = tearDown.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                        }

                        context.RegisterCodeFix(
                            $"Create {tearDown.Identifier.ValueText} method and dispose member.",
                            (editor, cancellationToken) => editor.InsertAfter(
                                setupMethod,
                                tearDown.AddBodyStatements(IDisposableFactory.DisposeStatement(left, editor.SemanticModel, cancellationToken))),
                            $"Create",
                            diagnostic);
                    }
                }

                bool TryGetMemberAccess(out SyntaxNode node)
                {
                    if (syntaxRoot.TryFindNode(diagnostic, out AssignmentExpressionSyntax assignment) &&
                        assignment is { Left: { } left })
                    {
                        node = left;
                        return true;
                    }

                    if (syntaxRoot.TryFindNode(diagnostic, out MemberDeclarationSyntax member))
                    {
                        node = member;
                        return true;
                    }

                    node = null;
                    return false;
                }
            }
        }

        private static MethodDeclarationSyntax CreateTearDownMethod(QualifiedType tearDownType)
        {
            var code = StringBuilderPool.Borrow()
                                        .AppendLine($"[{tearDownType.FullName}]")
                                        .AppendLine($"public void {tearDownType.Type.Replace("Attribute", string.Empty)}()")
                                        .AppendLine("{")
                                        .AppendLine("}")
                                        .Return();
            return Parse.MethodDeclaration(code)
                        .WithSimplifiedNames()
                        .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                        .WithTrailingTrivia(SyntaxFactory.ElasticMarker)
                        .WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
