namespace ValidCode.NetCore
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    public static class Issue308
    {
        private static readonly ConcurrentDictionary<int, object> Queue = new();

        public static unsafe void M1(ReadOnlySpan<byte> bytes)
        {
            M2<int>(1, bytes, &M3);
        }

        private static unsafe void M2<T>(int x, ReadOnlySpan<byte> bytes, delegate*<ReadOnlySpan<byte>, T> read)
        {
            if (Queue.TryRemove(1, out var tcs))
            {
                if (tcs is TaskCompletionSource<T> typed)
                {
                    try
                    {
                        typed.SetResult(read(bytes));
                    }
                    catch (Exception e)
                    {
                        typed.SetException(e);
                    }
                }
                else
                {
                    throw new InvalidOperationException("The response type does not match.");
                }
            }
        }

        private static int M3(ReadOnlySpan<byte> bytes) => 1;
    }
}
