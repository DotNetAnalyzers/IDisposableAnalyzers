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
            IDISP001DisposeCreated.Descriptor,
            IDISP003DisposeBeforeReassigning.Descriptor,
            IDISP008DontMixInjectedAndCreatedForMember.Descriptor);

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
                context.Node is AssignmentExpressionSyntax assignment &&
                !assignment.Left.IsKind(SyntaxKind.ElementAccessExpression) &&
                context.SemanticModel.TryGetSymbol(assignment.Left, context.CancellationToken, out ISymbol assignedSymbol))
            {
                if (LocalOrParameter.TryCreate(assignedSymbol, out var localOrParameter) &&
                    Disposable.IsCreation(assignment.Right, context.SemanticModel, context.CancellationToken).IsEither(Result.Yes, Result.AssumeYes) &&
                    Disposable.ShouldDispose(localOrParameter, assignment, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP001DisposeCreated.Descriptor, assignment.GetLocation()));
                }

                if (IsReassignedWithCreated(assignment, context))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP003DisposeBeforeReassigning.Descriptor, assignment.GetLocation()));
                }

                if (assignedSymbol is IParameterSymbol assignedParameter &&
                    assignedParameter.ContainingSymbol.DeclaredAccessibility != Accessibility.Private &&
                    assignedParameter.RefKind == RefKind.Ref &&
                    context.SemanticModel.TryGetType(assignment.Right, context.CancellationToken, out var type) &&
                    Disposable.IsAssignableFrom(type, context.Compilation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP008DontMixInjectedAndCreatedForMember.Descriptor, context.Node.GetLocation()));
                }
            }
        }

        private static bool IsReassignedWithCreated(AssignmentExpressionSyntax assignment, SyntaxNodeAnalysisContext context)
        {
            if (assignment.FirstAncestor<AccessorDeclarationSyntax>() is AccessorDeclarationSyntax accessor &&
                accessor.IsKind(SyntaxKind.SetAccessorDeclaration) &&
                assignment.Right is IdentifierNameSyntax assignedIdentifier &&
                assignedIdentifier.Identifier.ValueText == "value")
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
                TestFixture.IsAssignedAndDisposedInSetupAndTearDown(fieldOrProperty, context.Node.FirstAncestor<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken))
            {
                return false;
            }

            if (IsNullChecked(assignedSymbol, assignment, context.SemanticModel, context.CancellationToken))
            {
                return false;
            }

            if (TryGetAssignedLocal(out var local) &&
                Disposable.IsDisposedAfter(local, assignment, context.SemanticModel, context.CancellationToken))
            {
                return false;
            }

            return true;

            bool TryGetAssignedLocal(out ILocalSymbol result)
            {
                result = null;
                if (assignment.TryFirstAncestor(out MemberDeclarationSyntax memberDeclaration))
                {
                    using (var walker = VariableDeclaratorWalker.Borrow(memberDeclaration))
                    {
                        return walker.VariableDeclarators.TrySingle(
                                   x => context.SemanticModel.TryGetSymbol(
                                            x.Initializer?.Value, context.CancellationToken, out ISymbol symbol) &&
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
            return context.TryFirstAncestor(out IfStatementSyntax ifStatement) &&
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

                    case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression):
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
                using (var walker = AssignmentExecutionWalker.Borrow(nullCheck, Scope.Member, semanticModel, cancellationToken))
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
