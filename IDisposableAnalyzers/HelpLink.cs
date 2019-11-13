namespace IDisposableAnalyzers
{
    using System;

    [Obsolete("Remove")]
    internal static class HelpLink
    {
        internal static string ForId(string id)
        {
            return $"https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/{id}.md";
        }
    }
}
