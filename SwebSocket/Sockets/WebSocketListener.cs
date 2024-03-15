using System.Net;
using System.Net.Sockets;

namespace SwebSocket;

public class WebSocketListener : IDisposable
{
    public IPAddress Address { get; }
    public ushort Port { get; }

    private TcpListener listener;

    public WebSocketListener(IPAddress address, ushort port)
    {
        Address = address;
        Port = port;
        listener = new TcpListener(address, port);
        listener.Start();
    }
    public WebSocketListener(ushort port) : this(IPAddress.Any, port) { }
    public WebSocketListener(IPEndPoint endPoint) : this(endPoint.Address, (ushort)endPoint.Port) { }

    public WebSocket Accept() => new WebSocket(
        listener.AcceptTcpClient(),
        new ServerHandshake(),
        MaskingBehavior.UnmaskIncoming
    );

    public async Task<WebSocket> AcceptAsync(CancellationToken cancellationToken = default) => new WebSocket(
        await listener.AcceptTcpClientAsync(cancellationToken),
        new ServerHandshake(),
        MaskingBehavior.UnmaskIncoming
    );

    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                listener.Stop();
                listener.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}