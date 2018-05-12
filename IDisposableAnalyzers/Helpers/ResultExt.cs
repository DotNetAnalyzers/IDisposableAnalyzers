namespace IDisposableAnalyzers
{
    internal static class ResultExt
    {
        internal static bool IsEither(this Result result, Result first, Result other) => result == first || result == other;

        internal static bool IsEither(this Result result, Result first, Result second, Result third) => result == first || result == second || result == third;
    }
}