using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace SwebSocket;

public class WebSocketListener : IDisposable
{
    public IPAddress Address => options.Address!;
    public ushort Port => options.Port!.Value;
    public bool UseSsl => options.UseSsl;

    private ListenerOptions options { get; }

    private TcpListener listener;

    internal WebSocketListener(ListenerOptions options)
    {
        this.options = options;
        listener = new TcpListener(options.Address!, options.Port!.Value);
        listener.Start();
    }

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
        if (options.UseSsl)
        {
            var sslStream = new SslStream(client.GetStream());
            sslStream.AuthenticateAsServer(options.ServerCertificate!);
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