namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class AssignmentAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.IDISP001DisposeCreated,
            Descriptors.IDISP003DisposeBeforeReassigning,
            Descriptors.IDISP008DoNotMixInjectedAndCreatedForMember);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.SimpleAssignmentExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is AssignmentExpressionSyntax { Left: { } left, Right: { } right } assignment &&
                !left.IsKind(SyntaxKind.ElementAccessExpression) &&
                context.SemanticModel.TryGetSymbol(left, context.CancellationToken, out var assignedSymbol))
            {
                if (LocalOrParameter.TryCreate(assignedSymbol, out var localOrParameter) &&
                    Disposable.IsCreation(right, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                    DisposableWalker.ShouldDispose(localOrParameter, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP001DisposeCreated, assignment.GetLocation()));
                }

                if (IsReassignedWithCreated(assignment, context))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP003DisposeBeforeReassigning, assignment.GetLocation()));
                }

                if (assignedSymbol is IParameterSymbol { RefKind: RefKind.Ref } refParameter &&
                    refParameter.ContainingSymbol.DeclaredAccessibility != Accessibility.Private &&
                    context.SemanticModel.TryGetType(right, context.CancellationToken, out var type) &&
                    Disposable.IsAssignableFrom(type, context.Compilation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP008DoNotMixInjectedAndCreatedForMember, context.Node.GetLocation()));
                }
            }
        }

        private static bool IsReassignedWithCreated(AssignmentExpressionSyntax assignment, SyntaxNodeAnalysisContext context)
        {
            if (assignment.Right is IdentifierNameSyntax { Identifier: { ValueText: "value" } } &&
                assignment.FirstAncestor<AccessorDeclarationSyntax>() is { } accessor &&
                accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
            {
                return false;
            }

            if (Disposable.IsAlreadyAssignedWithCreated(assignment.Left, context.SemanticModel, context.CancellationToken, out var assignedSymbol)
                          .IsEither(Result.No, Result.AssumeNo, Result.Unknown))
            {
                return false;
            }

            if (assignedSymbol == KnownSymbol.SerialDisposable.Disposable ||
                assignedSymbol == KnownSymbol.SingleAssignmentDisposable.Disposable)
            {
                return false;
            }

            if (Disposable.IsDisposedBefore(assignedSymbol, assignment, context.SemanticModel, context.CancellationToken))
            {
                return false;
            }

            if (FieldOrProperty.TryCreate(assignedSymbol, out var fieldOrProperty) &&
                context.Node.FirstAncestor<TypeDeclarationSyntax>() is { } containingType &&
                TestFixture.IsAssignedAndDisposedInSetupAndTearDown(fieldOrProperty, containingType, context.SemanticModel, context.CancellationToken))
            {
                return false;
            }

            if (IsNullChecked(assignedSymbol, assignment, context.SemanticModel, context.CancellationToken))
            {
                return false;
            }

            if (TryGetAssignedLocal(out var local) &&
                DisposableWalker.DisposesAfter(local, assignment, context.SemanticModel, context.CancellationToken, null))
            {
                return false;
            }

            return true;

            bool TryGetAssignedLocal(out ILocalSymbol result)
            {
                result = null;
                if (assignment.TryFirstAncestor(out MemberDeclarationSyntax? memberDeclaration))
                {
                    using (var walker = VariableDeclaratorWalker.Borrow(memberDeclaration))
                    {
                        return walker.VariableDeclarators.TrySingle(
                                   x => x is { Initializer: { Value: { } value } } &&
                                        context.SemanticModel.TryGetSymbol(value, context.CancellationToken, out var symbol) &&
                                        symbol.Equals(assignedSymbol),
                                   out var match) &&
                               match.Initializer.Value.IsExecutedBefore(assignment) == ExecutedBefore.Yes &&
                               context.SemanticModel.TryGetSymbol(match, context.CancellationToken, out result);
                    }
                }

                return false;
            }
        }

        private static bool IsNullChecked(ISymbol symbol, SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return context.TryFirstAncestor(out IfStatementSyntax? ifStatement) &&
                   ifStatement.Statement.Contains(context) &&
                   IsNullCheck(ifStatement.Condition);

            bool IsNullCheck(ExpressionSyntax candidate)
            {
                switch (candidate)
                {
                    case IsPatternExpressionSyntax isPattern:
                        if (IsSymbol(isPattern.Expression) &&
                            isPattern.Pattern is ConstantPatternSyntax constantPattern &&
                            constantPattern.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                        {
                            return !IsAssignedBefore(ifStatement);
                        }

                        break;

                    case BinaryExpressionSyntax binary
                        when binary.IsKind(SyntaxKind.EqualsExpression):
                        if (binary.Left.IsKind(SyntaxKind.NullLiteralExpression) &&
                            IsSymbol(binary.Right))
                        {
                            return !IsAssignedBefore(ifStatement);
                        }

                        if (IsSymbol(binary.Left) &&
                            binary.Right.IsKind(SyntaxKind.NullLiteralExpression))
                        {
                            return !IsAssignedBefore(ifStatement);
                        }

                        break;
                    case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression):
                        return IsNullCheck(binary.Left) || IsNullCheck(binary.Right);
                    case InvocationExpressionSyntax invocation:
                        if (invocation.Expression is IdentifierNameSyntax identifierName &&
                            invocation.ArgumentList != null &&
                            invocation.ArgumentList.Arguments.Count == 2 &&
                            (identifierName.Identifier.ValueText == nameof(ReferenceEquals) ||
                             identifierName.Identifier.ValueText == nameof(Equals)))
                        {
                            if (invocation.ArgumentList.Arguments.TrySingle(x => x.Expression?.IsKind(SyntaxKind.NullLiteralExpression) == true, out _) &&
                                invocation.ArgumentList.Arguments.TrySingle(x => IsSymbol(x.Expression), out _))
                            {
                                return !IsAssignedBefore(ifStatement);
                            }
                        }
                        else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                                 memberAccess.Name is IdentifierNameSyntax memberIdentifier &&
                                 invocation.ArgumentList != null &&
                                 invocation.ArgumentList.Arguments.Count == 2 &&
                                 (memberIdentifier.Identifier.ValueText == nameof(ReferenceEquals) ||
                                  memberIdentifier.Identifier.ValueText == nameof(Equals)))
                        {
                            if (invocation.ArgumentList.Arguments.TrySingle(x => x.Expression?.IsKind(SyntaxKind.NullLiteralExpression) == true, out _) &&
                                invocation.ArgumentList.Arguments.TrySingle(x => IsSymbol(x.Expression), out _))
                            {
                                return !IsAssignedBefore(ifStatement);
                            }
                        }

                        break;
                }

                return false;
            }

            bool IsSymbol(ExpressionSyntax expression)
            {
                if (expression is IdentifierNameSyntax identifierName)
                {
                    return identifierName.Identifier.ValueText == symbol.Name;
                }

                if (symbol.IsEitherKind(SymbolKind.Field, SymbolKind.Property) &&
                    expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression is InstanceExpressionSyntax &&
                    memberAccess.Name is IdentifierNameSyntax identifier)
                {
                    return identifier.Identifier.ValueText == symbol.Name;
                }

                return false;
            }

            bool IsAssignedBefore(IfStatementSyntax nullCheck)
            {
                using (var walker = AssignmentExecutionWalker.Borrow(nullCheck, SearchScope.Member, semanticModel, cancellationToken))
                {
                    foreach (var assignment in walker.Assignments)
                    {
                        if (IsSymbol(assignment.Left) &&
                            assignment.SpanStart < context.SpanStart)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
