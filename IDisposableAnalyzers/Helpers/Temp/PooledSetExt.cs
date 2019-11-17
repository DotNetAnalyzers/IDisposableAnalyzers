namespace IDisposableAnalyzers
{
    using System;
    using System.Runtime.CompilerServices;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;

    [Obsolete("Use Recursion")]
    internal static class PooledSetExt
    {
        internal static bool CanVisit(this PooledSet<(string Caller, SyntaxNode Node)>? visited, SyntaxNode node, out PooledSet<(string Caller, SyntaxNode Node)> incremented, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = 0)
        {
            incremented = visited.IncrementUsage();
            return incremented.Add((caller ?? string.Empty + line.ToString(), node));
        }
    }
}
