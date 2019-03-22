namespace IDisposableAnalyzers
{
    using System.Runtime.CompilerServices;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;

    internal static class PooledSetExt
    {
        internal static bool CanVisit(this PooledSet<(string, SyntaxNode)> visited, SyntaxNode node, out PooledSet<(string, SyntaxNode)> incremented, [CallerMemberName] string caller = null)
        {
            incremented = visited.IncrementUsage();
            return incremented.Add((caller ?? string.Empty, node));
        }
    }
}
