namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class FieldAndPropertyDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.IDISP002DisposeMember,
            Descriptors.IDISP006ImplementIDisposable,
            Descriptors.IDISP008DoNotMixInjectedAndCreatedForMember);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => HandleField(c), SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(c => HandleProperty(c), SyntaxKind.PropertyDeclaration);
        }

        private static void HandleField(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.ContainingSymbol is IFieldSymbol { IsStatic: false, IsConst: false } field &&
                context.Node is FieldDeclarationSyntax declaration &&
                Disposable.IsPotentiallyAssignableFrom(field.Type, context.Compilation))
            {
                HandleFieldOrProperty(context, new FieldOrPropertyAndDeclaration(field, declaration));
            }
        }

        private static void HandleProperty(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.ContainingSymbol is IPropertySymbol { IsStatic: false, IsIndexer: false } property &&
                context.Node is PropertyDeclarationSyntax { AccessorList: { Accessors: { } accessors } } declaration &&
                accessors.FirstOrDefault() is { Body: null, ExpressionBody: null } &&
                Disposable.IsPotentiallyAssignableFrom(property.Type, context.Compilation))
            {
                HandleFieldOrProperty(context, new FieldOrPropertyAndDeclaration(property, declaration));
            }
        }

        private static void HandleFieldOrProperty(SyntaxNodeAnalysisContext context, FieldOrPropertyAndDeclaration member)
        {
            using var assignedValues = AssignedValueWalker.Borrow(member.FieldOrProperty.Symbol, context.SemanticModel, context.CancellationToken);
            using var recursive = RecursiveValues.Borrow(assignedValues, context.SemanticModel, context.CancellationToken);
            if (Disposable.IsAnyCreation(recursive, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes))
            {
                if (Disposable.IsAnyCachedOrInjected(recursive, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) ||
                    IsMutableFromOutside(member.FieldOrProperty))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP008DoNotMixInjectedAndCreatedForMember, context.Node.GetLocation()));
                }
                else if (TestFixture.IsAssignedInInitialize(member, context.SemanticModel, context.CancellationToken, out _, out var setupAttribute))
                {
                    switch (TestFixture.FindTearDown(setupAttribute!, context.SemanticModel, context.CancellationToken))
                    {
                        case { } tearDown
                            when !DisposableMember.IsDisposed(member.FieldOrProperty, tearDown, context.SemanticModel, context.CancellationToken):
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.IDISP002DisposeMember,
                                    context.Node.GetLocation(),
                                    additionalLocations: new[] { tearDown.GetLocation() }));
                            break;
                        case null:
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP002DisposeMember, context.Node.GetLocation()));
                            break;
                    }
                }
                else
                {
                    if (DisposeMethod.FindDisposeAsync(member.FieldOrProperty.ContainingType, context.Compilation, Search.TopLevel) is { } disposeAsync &&
                        !DisposableMember.IsDisposed(member.FieldOrProperty, disposeAsync, context.SemanticModel, context.CancellationToken))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.IDISP002DisposeMember,
                                context.Node.GetLocation(),
                                additionalLocations: disposeAsync.Locations));
                    }

                    if (DisposeMethod.FindFirst(member.FieldOrProperty.ContainingType, context.Compilation, Search.TopLevel) is { } dispose &&
                        !DisposableMember.IsDisposed(member.FieldOrProperty, dispose, context.SemanticModel, context.CancellationToken))
                    {
                        dispose = DisposeMethod.FindVirtual(member.FieldOrProperty.ContainingType, context.Compilation, Search.TopLevel) ?? dispose;
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.IDISP002DisposeMember,
                                context.Node.GetLocation(),
                                additionalLocations: dispose.Locations));
                    }

                    if (DisposableMember.IsDisposed(member, context.SemanticModel, context.CancellationToken).IsEither(Result.No, Result.AssumeNo) &&
                        DisposeMethod.FindFirst(member.FieldOrProperty.ContainingType, context.Compilation, Search.TopLevel) is null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP006ImplementIDisposable, member.Declaration.GetLocation()));
                    }
                }
            }
        }

        private static bool IsMutableFromOutside(FieldOrProperty fieldOrProperty)
        {
            return fieldOrProperty.Symbol switch
            {
                IFieldSymbol { IsReadOnly: true } => false,
                IFieldSymbol field
                => IsAccessible(field.DeclaredAccessibility, field.ContainingType),
                IPropertySymbol property
                => IsAccessible(property.DeclaredAccessibility, property.ContainingType) &&
                   property.SetMethod is { } set &&
                   IsAccessible(set.DeclaredAccessibility, property.ContainingType),
                _ => throw new InvalidOperationException("Should not get here."),
            };

            static bool IsAccessible(Accessibility accessibility, INamedTypeSymbol containingType)
            {
                return accessibility switch
                {
                    Accessibility.Private => false,
                    Accessibility.Protected => !containingType.IsSealed,
                    Accessibility.Internal => true,
                    Accessibility.ProtectedOrInternal => true,
                    Accessibility.ProtectedAndInternal => true,
                    Accessibility.Public => true,
                    _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, "Unhandled accessibility")
                };
            }
        }
    }
}
