namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class AssignmentAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            IDISP003DisposeBeforeReassigning.Descriptor,
            IDISP008DontMixInjectedAndCreatedForMember.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void HandleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is AssignmentExpressionSyntax assignment)
            {
                if (context.SemanticModel.GetSymbolSafe(assignment.Left, context.CancellationToken) is IParameterSymbol parameter &&
                    parameter.ContainingSymbol.DeclaredAccessibility != Accessibility.Private &&
                    parameter.RefKind == RefKind.Ref &&
                    Disposable.IsAssignableTo(context.SemanticModel.GetTypeInfoSafe(assignment.Right, context.CancellationToken).Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP008DontMixInjectedAndCreatedForMember.Descriptor, context.Node.GetLocation()));
                }

                if (IsReassignedWithCreated(assignment, context))
                {
                    context.ReportDiagnostic(Diagnostic.Create(IDISP003DisposeBeforeReassigning.Descriptor, assignment.GetLocation()));
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

            if (Disposable.IsAssignedWithCreated(assignment.Left, context.SemanticModel, context.CancellationToken, out var assignedSymbol)
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

            if (TestFixture.IsAssignedAndDisposedInSetupAndTearDown(assignedSymbol, context.Node.FirstAncestor<TypeDeclarationSyntax>(), context.SemanticModel, context.CancellationToken))
            {
                return false;
            }

            if (IsNullChecked(assignedSymbol, assignment))
            {
                return false;
            }

            return true;
        }

        private static bool IsNullChecked(ISymbol symbol, SyntaxNode context)
        {
            bool IsSymbol(ExpressionSyntax expression)
            {
                if (expression is IdentifierNameSyntax identifierName)
                {
                    return identifierName.Identifier.ValueText == symbol.Name;
                }

                if (symbol.IsEither<IFieldSymbol, IPropertySymbol>() &&
                    expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression is InstanceExpressionSyntax &&
                    memberAccess.Name is IdentifierNameSyntax identifier)
                {
                    return identifier.Identifier.ValueText == symbol.Name;
                }

                return false;
            }

            var ifStatement = context.FirstAncestor<IfStatementSyntax>();
            if (ifStatement == null)
            {
                return false;
            }

            if (!ifStatement.Statement.Contains(context))
            {
                return false;
            }

            if (ifStatement.Condition is BinaryExpressionSyntax binary &&
                binary.IsKind(SyntaxKind.EqualsExpression))
            {
                if (binary.Left.IsKind(SyntaxKind.NullLiteralExpression) &&
                    IsSymbol(binary.Right))
                {
                    return true;
                }

                if (IsSymbol(binary.Left) &&
                    binary.Right.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    return true;
                }
            }
            else if (ifStatement.Condition is InvocationExpressionSyntax invocation)
            {
                if (invocation.Expression is IdentifierNameSyntax identifierName &&
                    invocation.ArgumentList != null &&
                    invocation.ArgumentList.Arguments.Count == 2 &&
                    (identifierName.Identifier.ValueText == "ReferenceEquals" ||
                     identifierName.Identifier.ValueText == "Equals"))
                {
                    if (invocation.ArgumentList.Arguments.TryGetSingle(x => x.Expression?.IsKind(SyntaxKind.NullLiteralExpression) == true, out _) &&
                        invocation.ArgumentList.Arguments.TryGetSingle(x => IsSymbol(x.Expression), out _))
                    {
                        return true;
                    }
                }
                else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                         memberAccess.Name is IdentifierNameSyntax memberIdentifier &&
                        invocation.ArgumentList != null &&
                        invocation.ArgumentList.Arguments.Count == 2 &&
                        (memberIdentifier.Identifier.ValueText == "ReferenceEquals" ||
                         memberIdentifier.Identifier.ValueText == "Equals"))
                {
                    if (invocation.ArgumentList.Arguments.TryGetSingle(x => x.Expression?.IsKind(SyntaxKind.NullLiteralExpression) == true, out _) &&
                        invocation.ArgumentList.Arguments.TryGetSingle(x => IsSymbol(x.Expression), out _))
                    {
                        return true;
                    }
                }
            }

            return IsNullChecked(symbol, ifStatement);
        }
    }
}
