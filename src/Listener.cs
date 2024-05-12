
using System.Net;
using System.Net.Sockets;

namespace SwebSocket;

public class Listener
{
    public bool Pending => listener.Pending();

    private TcpListener listener;
    private SslOptions sslOptions = SslOptions.NoSsl;

    public Listener(IPAddress address, ushort port)
    {
        listener = new TcpListener(address, port);
        Start();
    }

    public void Start() => listener.Start();
    public void Stop() => listener.Stop();

    public Listener UseSsl(SslOptions sslOptions)
    {
        this.sslOptions = sslOptions;
        return this;
    }

    public WebSocket Accept()
    {
        var socket = listener.AcceptSocket();
        var stream = sslOptions.SslStreamFactory.Invoke(new NetworkStream(socket));
        var frameSocket = new FrameSocket(socket, stream, false);
        var connectionFrameSocket = new ConnectionFrameSocket(frameSocket, new ServerHandshake(stream));
        return new WebSocket(connectionFrameSocket);
    }

    public bool TryAccept(out WebSocket? webSocket)
    {
        webSocket = Pending ? Accept() : null;
        return webSocket != null;
    }
}