
using System.Net;
using System.Net.Sockets;

namespace SwebSocket;

public class Listener
{
    /// <summary>
    /// Wether there is an incoming connection
    /// </summary>
    public bool Pending => listener.Pending();

    private TcpListener listener;
    private SslOptions sslOptions = SslOptions.NoSsl;

    /// <summary>
    /// Create a new listener on the specified address and port.
    /// The listener is started immediately, see <see cref="Start"/> for more info.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="port"></param>
    public Listener(IPAddress address, ushort port)
    {
        listener = new TcpListener(address, port);
        Start();
    }

    /// <summary>
    /// Start the listener on the address and port specified in the constructor.
    /// </summary>
    /// <exception cref="SocketException">If the listener could not be started</exception>
    public void Start() => listener.Start();

    /// <summary>
    /// Stop the listener.
    /// </summary>
    /// <exception cref="SocketException">If the listener could not be stopped</exception>
    public void Stop() => listener.Stop();

    /// <summary>
    /// Use SSL for the connection. The port is left unchanged.
    /// </summary>
    /// <param name="sslOptions"></param>
    public Listener UseSsl(SslOptions sslOptions)
    {
        this.sslOptions = sslOptions;
        return this;
    }

    /// <summary>
    /// Accept an incoming connection.
    /// </summary>
    /// <exception cref="SocketException">Listener was closed whilst waiting for an incoming Connection</exception>
    public WebSocket Accept()
    {
        var socket = listener.AcceptSocket();
        var stream = sslOptions.SslStreamFactory.Invoke(new NetworkStream(socket));
        var frameSocket = new FrameSocket(socket, stream, false);
        var connectionFrameSocket = new ConnectionFrameSocket(frameSocket, new ServerHandshake(stream));
        return new WebSocket(connectionFrameSocket);
    }

    /// <summary>
    /// Try to accept an incoming connection.
    /// </summary>
    /// <returns>Whether a WebSocket was accepted or not</returns>
    /// <exception cref="SocketException">The listener is closed</exception>
    public bool TryAccept(out WebSocket? webSocket)
    {
        webSocket = Pending ? Accept() : null;
        return webSocket != null;
    }
}