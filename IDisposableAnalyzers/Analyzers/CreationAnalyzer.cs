namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CreationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.IDISP004DoNotIgnoreCreated,
            Descriptors.IDISP014UseSingleInstanceOfHttpClient);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ObjectCreationExpression, SyntaxKind.InvocationExpression, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.ConditionalAccessExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                ShouldCheck(context) is { } expression)
            {
                if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                    context.SemanticModel.TryGetType(objectCreation, context.CancellationToken, out var type) &&
                    type.IsAssignableTo(KnownSymbol.HttpClient, context.Compilation) &&
                    !IsStaticFieldInitializer(objectCreation) &&
                    !IsStaticPropertyInitializer(objectCreation) &&
                    !IsStaticCtor(context.ContainingSymbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP014UseSingleInstanceOfHttpClient, objectCreation.GetLocation()));
                }

                if (Disposable.IsCreation(expression, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                    DisposableWalker.Ignores(expression, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP004DoNotIgnoreCreated, context.Node.GetLocation()));
                }
            }
        }

        private static ExpressionSyntax? ShouldCheck(SyntaxNodeAnalysisContext context)
        {
            return context.Node switch
            {
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: { } expression, Name: { Identifier: { ValueText: "Schedule" } } } }
                when context.SemanticModel.TryGetNamedType(expression, context.CancellationToken, out var type) &&
                     type.IsAssignableTo(KnownSymbol.RxIScheduler, context.SemanticModel.Compilation)
                => null,
                InvocationExpressionSyntax invocation
                => invocation,
                ObjectCreationExpressionSyntax objectCreation
                => objectCreation,
                MemberAccessExpressionSyntax { Expression: { } expression }
                    when context.SemanticModel.TryGetSymbol(expression, context.CancellationToken, out IPropertySymbol? property) &&
                         Disposable.IsPotentiallyAssignableFrom(property.Type, context.Compilation)
                => expression,
                ConditionalAccessExpressionSyntax { Expression: { } expression }
                    when context.SemanticModel.TryGetSymbol(expression, context.CancellationToken, out IPropertySymbol? property) &&
                         Disposable.IsPotentiallyAssignableFrom(property.Type, context.Compilation)
                => expression,
                _ => null,
            };
        }

        private static bool IsStaticFieldInitializer(ObjectCreationExpressionSyntax objectCreation)
        {
            return objectCreation.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax fieldDeclaration } } } &&
                   fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
        }

        private static bool IsStaticPropertyInitializer(ObjectCreationExpressionSyntax objectCreation)
        {
            return objectCreation.Parent is EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax propertyDeclaration } &&
                   propertyDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
        }

        private static bool IsStaticCtor(ISymbol containingSymbol) => containingSymbol is IMethodSymbol { IsStatic: true, MethodKind: MethodKind.SharedConstructor };
    }
}
