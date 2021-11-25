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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeInTearDownFix))]
    [Shared]
    internal class DisposeInTearDownFix : DocumentEditorCodeFixProvider
    {
        private static readonly MethodDeclarationSyntax TearDownMethod = CreateTearDownMethod(KnownSymbols.NUnit.TearDownAttribute);
        private static readonly MethodDeclarationSyntax OneTimeTearDownMethod = CreateTearDownMethod(KnownSymbols.NUnit.OneTimeTearDownAttribute);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.IDISP002DisposeMember.Id,
            Descriptors.IDISP003DisposeBeforeReassigning.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (FindMemberAccess() is { } member &&
                    semanticModel is { } &&
                    semanticModel.TryGetSymbol(member, context.CancellationToken, out ISymbol? memberSymbol) &&
                    FieldOrProperty.TryCreate(memberSymbol, out var fieldOrProperty) &&
                    member.FirstAncestor<ClassDeclarationSyntax>() is { } classDeclaration &&
                    InitializeAndCleanup.IsAssignedInInitialize(fieldOrProperty, classDeclaration, semanticModel, context.CancellationToken, out var assignment, out var initialize) &&
                    assignment is { Left: { } left })
                {
                    if (InitializeAndCleanup.FindCleanup(initialize, semanticModel, context.CancellationToken) is { } cleanup)
                    {
                        switch (cleanup)
                        {
                            case { Body: { } body }:
                                context.RegisterCodeFix(
                                    $"Dispose member in {cleanup.Identifier.ValueText}.",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        body,
                                        x => x.AddStatements(IDisposableFactory.DisposeStatement(left, editor.SemanticModel, cancellationToken))),
                                    $"Dispose member in {cleanup.Identifier.ValueText}.",
                                    diagnostic);
                                break;
                            case { ExpressionBody: { Expression: { } expression } }:
                                context.RegisterCodeFix(
                                    $"Dispose member in {cleanup.Identifier.ValueText}.",
                                    (editor, cancellationToken) => editor.ReplaceNode(
                                        cleanup,
                                        x => x.AsBlockBody(
                                            SyntaxFactory.ExpressionStatement(expression),
                                            IDisposableFactory.DisposeStatement(left, editor.SemanticModel, cancellationToken))),
                                    $"Dispose member in {cleanup.Identifier.ValueText}.",
                                    diagnostic);
                                break;
                        }
                    }
                    else if (TearDown() is { } tearDown)
                    {
                        context.RegisterCodeFix(
                            $"Create {tearDown.Identifier.ValueText} method and Dispose member",
                            (editor, cancellationToken) => editor.InsertAfter(
                                initialize,
                                tearDown.AddBodyStatements(IDisposableFactory.DisposeStatement(left, editor.SemanticModel, cancellationToken))),
                            $"Create",
                            diagnostic);
                    }

                    MethodDeclarationSyntax? TearDown()
                    {
                        if (Attribute.TryFind(initialize!, KnownSymbols.NUnit.SetUpAttribute, semanticModel, context.CancellationToken, out _))
                        {
                            return AdjustStatic(TearDownMethod);
                        }

                        if (Attribute.TryFind(initialize!, KnownSymbols.NUnit.OneTimeSetUpAttribute, semanticModel, context.CancellationToken, out _))
                        {
                            return AdjustStatic(OneTimeTearDownMethod);
                        }

                        return null;

                        MethodDeclarationSyntax AdjustStatic(MethodDeclarationSyntax method)
                        {
                            return initialize!.Modifiers.Any(SyntaxKind.StaticKeyword)
                                ? method.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                : method;
                        }
                    }
                }

                SyntaxNode? FindMemberAccess()
                {
                    return syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) switch
                    {
                        AssignmentExpressionSyntax { Left: { } l } => l,
                        MemberDeclarationSyntax m => m,
                        _ => null,
                    };
                }
            }
        }

        private static MethodDeclarationSyntax CreateTearDownMethod(QualifiedType tearDownType)
        {
            return SyntaxFactory.MethodDeclaration(
                attributeLists: SyntaxFactory.SingletonList(
                    SyntaxFactory.AttributeList(
                        openBracketToken: SyntaxFactory.Token(SyntaxKind.OpenBracketToken),
                        target: default,
                        attributes: SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(
                                name: SyntaxFactory.QualifiedName(
                                    left: SyntaxFactory.QualifiedName(
                                        left: SyntaxFactory.IdentifierName("NUnit"),
                                        dotToken: SyntaxFactory.Token(SyntaxKind.DotToken),
                                        right: SyntaxFactory.IdentifierName("Framework")),
                                    dotToken: SyntaxFactory.Token(SyntaxKind.DotToken),
                                    right: SyntaxFactory.IdentifierName(tearDownType.Alias))
                                                   .WithSimplifiedNames(),
                                argumentList: default)),
                        closeBracketToken: SyntaxFactory.Token(SyntaxKind.CloseBracketToken))),
                modifiers: SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                returnType: SyntaxFactory.PredefinedType(
                    keyword: SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                explicitInterfaceSpecifier: default,
                identifier: SyntaxFactory.Identifier(tearDownType.Alias),
                typeParameterList: default,
                parameterList: SyntaxFactory.ParameterList(),
                constraintClauses: default,
                body: SyntaxFactory.Block(),
                expressionBody: default,
                semicolonToken: default);
        }
    }
}
