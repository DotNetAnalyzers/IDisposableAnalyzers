namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        [Obsolete("Use DisposableWalker")]
        private static bool TryGetAssignedFieldOrProperty(ArgumentSyntax argument, IMethodSymbol method, SemanticModel semanticModel, CancellationToken cancellationToken, out FieldOrProperty member)
        {
            member = default(FieldOrProperty);
            if (method == null)
            {
                return false;
            }

            if (method.TryFindParameter(argument, out var parameter))
            {
                if (method.TrySingleDeclaration(cancellationToken, out BaseMethodDeclarationSyntax methodDeclaration))
                {
                    if (AssignmentExecutionWalker.FirstWith(parameter.OriginalDefinition, (SyntaxNode)methodDeclaration.Body ?? methodDeclaration.ExpressionBody, Scope.Member, semanticModel, cancellationToken, out var assignment))
                    {
                        return semanticModel.TryGetSymbol(assignment.Left, cancellationToken, out ISymbol symbol) &&
                               FieldOrProperty.TryCreate(symbol, out member);
                    }

                    if (methodDeclaration is ConstructorDeclarationSyntax ctor &&
                        ctor.Initializer is ConstructorInitializerSyntax initializer &&
                        initializer.ArgumentList != null &&
                        initializer.ArgumentList.Arguments.TrySingle(x => x.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == parameter.Name, out var chainedArgument) &&
                        semanticModel.TryGetSymbol(initializer, cancellationToken, out var chained))
                    {
                        return TryGetAssignedFieldOrProperty(chainedArgument, chained, semanticModel, cancellationToken, out member);
                    }
                }
                else if (method == KnownSymbol.Tuple.Create)
                {
                    return method.ReturnType.TryFindProperty(parameter.Name.ToFirstCharUpper(), out var field) &&
                           FieldOrProperty.TryCreate(field, out member);
                }
                else if (method.MethodKind == MethodKind.Constructor &&
                         method.ContainingType.MetadataName.StartsWith("Tuple`"))
                {
                    return method.ContainingType.TryFindProperty(parameter.Name.ToFirstCharUpper(), out var field) &&
                           FieldOrProperty.TryCreate(field, out member);
                }
            }

            return false;
        }
    }
}
