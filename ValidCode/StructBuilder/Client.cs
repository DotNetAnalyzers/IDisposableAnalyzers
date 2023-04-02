using System.Threading;
using System;
using System.Net.Sockets;

namespace ValidCode.StructBuilder;

using System.Net;
using System.Threading.Tasks;

internal sealed class Client : IDisposable
{
    private readonly Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    private bool disposed;

    public async Task<int> M(bool rth)
    {
        this.ThrowIfDisposed();
        var id = Request.GetNextValidId();
        using var request = new Request()
            .Append(6,             "message")
            .Append(2,             "version")
            .Append(true,          "subscribe")
            .Append(rth,           "useRTH")
            .Append(!rth,           "not")
            .Append("abc",         "text")
            .Append((string?)null, "empty");
        await this.socket.SendAsync(request.LengthPrefixed(), SocketFlags.None, CancellationToken.None).ConfigureAwait(false);
        return id;
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.socket.Dispose();
    }

    internal Task ConnectAsync(int port)
    {
        return this.socket.ConnectAsync(new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), port));
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(nameof(Client));
        }
    }
}
