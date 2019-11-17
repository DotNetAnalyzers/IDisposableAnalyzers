namespace IDisposableAnalyzers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Obsolete("Use Gu.Roslyn.Extensions")]
    internal static class ObjectCreationExpressionSyntaxExt
    {
        internal static bool IsType(this ObjectCreationExpressionSyntax objectCreation, QualifiedType type, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return objectCreation switch
            {
                { Type: IdentifierNameSyntax identifierName } => identifierName == type &&
                                                                 semanticModel.TryGetType(objectCreation, cancellationToken, out var symbol) &&
                                                                 symbol == type,
                _ => semanticModel.TryGetType(objectCreation, cancellationToken, out var symbol) &&
                     symbol == type,
            };
        }
    }
}
