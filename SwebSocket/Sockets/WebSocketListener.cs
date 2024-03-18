using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace SwebSocket;

public class WebSocketListener : IDisposable
{
    public IPAddress Address { get; }
    public ushort Port { get; }
    public ServerSSLOptions SSLOptions { get; set; }

    private TcpListener listener;

    public WebSocketListener(IPAddress address, ushort port)
        : this(address, port, ServerSSLOptions.NoSSL()) { }
    public WebSocketListener(IPAddress address, ushort port, ServerSSLOptions sSLOptions)
    {
        Address = address;
        Port = port;
        SSLOptions = sSLOptions;
        listener = new TcpListener(address, port);
        listener.Start();
    }
    public WebSocketListener(ushort port) : this(IPAddress.Any, port) { }
    public WebSocketListener(ushort port, ServerSSLOptions options) : this(IPAddress.Any, port, options) { }
    public WebSocketListener(IPEndPoint endPoint) : this(endPoint.Address, (ushort)endPoint.Port) { }
    public WebSocketListener(IPEndPoint endPoint, ServerSSLOptions sSLOptions) : this(endPoint.Address, (ushort)endPoint.Port, sSLOptions) { }

    public WebSocket Accept()
    {
        var client = listener.AcceptTcpClient();
        return new WebSocket(
            client,
            GetStream(client),
            new ServerHandshake(),
            MaskingBehavior.UnmaskIncoming
        );
    }

    public async Task<WebSocket> AcceptAsync(CancellationToken cancellationToken = default)
    {
        var client = await listener.AcceptTcpClientAsync(cancellationToken);
        return new WebSocket(
            client,
            GetStream(client),
            new ServerHandshake(),
            MaskingBehavior.UnmaskIncoming
        );
    }

    private Stream GetStream(TcpClient client)
    {
        if (SSLOptions.UseSSL)
        {
            var sslStream = new SslStream(client.GetStream());
            sslStream.AuthenticateAsServer(SSLOptions.Certificate!);
            return sslStream;
        }
        return client.GetStream();
    }

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