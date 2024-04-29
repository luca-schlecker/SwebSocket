
using System;
using System.Net.Sockets;

namespace SwebSocket;

public class WebSocketBuilder
{
    private Uri? uri;
    private SslOptions sslOptions = SslOptions.NoSsl;

    public WebSocketBuilder To(string url) => To(new Uri(url));
    public WebSocketBuilder To(Uri uri)
    {
        this.uri = uri;
        if (uri.Scheme != "ws" && uri.Scheme != "wss")
            throw new ArgumentException("Invalid scheme. Expected 'ws' or 'wss'.");
        if (uri.Scheme == "wss")
            return UseSsl();
        else
            return this;
    }

    public WebSocketBuilder UseSsl(SslOptions? sslOptions)
    {
        if (sslOptions == null) return UseSsl();
        this.sslOptions = sslOptions;
        return this;
    }

    public WebSocketBuilder UseSsl()
    {
        if (uri == null)
            throw new InvalidOperationException("Cannot build WebSocket without a URL.");

        sslOptions = SslOptions.ForClient(uri.Host);
        return this;
    }

    public WebSocket Build()
    {
        if (uri == null)
            throw new InvalidOperationException("Cannot build WebSocket without a URL.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(uri.Host, uri.Port);
        var stream = sslOptions.SslStreamFactory.Invoke(new NetworkStream(socket));
        var frameSocket = new FrameSocket(socket, stream, true);
        var handshake = new ClientHandshake(stream, uri.Host, (ushort)uri.Port, uri.AbsolutePath);
        var ConnectionFrameSocket = new ConnectionFrameSocket(frameSocket, handshake);
        return new WebSocket(ConnectionFrameSocket);
    }
}