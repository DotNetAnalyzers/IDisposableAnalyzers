namespace IDisposableAnalyzers
{
    using System.Threading.Tasks;

    internal static class FinishedTasks
    {
        internal static Task Task { get; } = Task.FromResult(default(VoidResult));

        private struct VoidResult
        {
        }
    }
}