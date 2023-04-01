namespace ValidCode.StructBuilder;

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics;
using System.Threading;

[DebuggerDisplay("{ToString(),nq}")]
public struct Request : IDisposable
{
    private static int id;

    private readonly byte[] bytes = ArrayPool<byte>.Shared.Rent(1024);
    private int position = 4;

    public Request()
    {
    }

    public void Dispose() => ArrayPool<byte>.Shared.Return(this.bytes);

    internal static int GetNextValidId() => Interlocked.Increment(ref id);

#pragma warning disable IDE0060 // Remove unused parameter
    internal Request Append(string? value, string? name = null)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        if (value is not null)
        {
            foreach (var c in value)
            {
                this.bytes[this.position] = (byte)c;
                this.position++;
            }
        }

        this.bytes[this.position] = 0;
        this.position++;
        return this;
    }

    internal Request Append(int value, string? name = null)
    {
        if (Utf8Formatter.TryFormat(value, this.bytes.AsSpan(this.position), out var bytesWritten))
        {
            this.position += bytesWritten;
            this.bytes[this.position] = 0;
            this.position++;
            return this;
        }

        throw new InvalidOperationException($"error writing {name}");
    }

    internal Request Append(bool value, string? name = null) => this.Append(value ? 1 : 0, name);

    internal ReadOnlyMemory<byte> LengthPrefixed()
    {
        BinaryPrimitives.WriteInt32BigEndian(this.bytes.AsSpan(0), this.position - 4);
        return new(this.bytes, 0, this.position);
    }
}
