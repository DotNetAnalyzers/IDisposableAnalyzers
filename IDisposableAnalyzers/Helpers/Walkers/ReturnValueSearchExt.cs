namespace IDisposableAnalyzers
{
    internal static class ReturnValueSearchExt
    {
        public static bool IsEither(this ReturnValueSearch search, ReturnValueSearch search1, ReturnValueSearch search2) => search == search1 || search == search2;
    }
}