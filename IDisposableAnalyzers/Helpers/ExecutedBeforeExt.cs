namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    internal static class ExecutedBeforeExt
    {
        internal static bool IsEither(this ExecutedBefore result, ExecutedBefore first, ExecutedBefore other) => result == first || result == other;
    }
}