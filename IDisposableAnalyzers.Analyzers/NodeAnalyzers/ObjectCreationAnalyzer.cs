namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ObjectCreationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP004DontIgnoreReturnValueOfTypeIDisposable.Descriptor,
            IDISP014UseSingleInstanceOfHttpClient.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ObjectCreationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation)
            {
                if (objectCreation.Type == KnownSymbol.HttpClient &&
                    !IsStaticFieldInitializer(objectCreation) &&
                    !IsStaticPropertyInitializer(objectCreation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP014UseSingleInstanceOfHttpClient.Descriptor, objectCreation.GetLocation()));
                }

                if (Disposable.IsCreation(objectCreation, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                    Disposable.IsIgnored(objectCreation, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP004DontIgnoreReturnValueOfTypeIDisposable.Descriptor, context.Node.GetLocation()));
                }
            }
        }

        private static bool IsStaticFieldInitializer(ObjectCreationExpressionSyntax objectCreation)
        {
            return objectCreation.Parent is EqualsValueClauseSyntax equalsValueClause &&
                   equalsValueClause.Parent is VariableDeclaratorSyntax variableDeclarator &&
                   variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration &&
                   variableDeclaration.Parent is FieldDeclarationSyntax fieldDeclaration &&
                   fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
        }

        private static bool IsStaticPropertyInitializer(ObjectCreationExpressionSyntax objectCreation)
        {
            return objectCreation.Parent is EqualsValueClauseSyntax equalsValueClause &&
                   equalsValueClause.Parent is PropertyDeclarationSyntax propertyDeclaration &&
                   propertyDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
        }
    }
}
