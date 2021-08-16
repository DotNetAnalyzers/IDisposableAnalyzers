namespace ValidCode.NetCore
{
    using System;

    public static class Issue308
    {
        public static unsafe void M1(ReadOnlySpan<byte> bytes)
        {
            _ = M2<int>(bytes, &M3);
        }

        private static unsafe T M2<T>(ReadOnlySpan<byte> bytes, delegate*<ReadOnlySpan<byte>, T> read)
        {
            return read(bytes);
        }

        private static int M3(this ReadOnlySpan<byte> bytes) => 1;
    }
}
