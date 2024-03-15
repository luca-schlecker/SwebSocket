using System.Net.Sockets;

namespace SwebSocket;

internal abstract class Handshake
{
    public abstract Task StartHandshake(TcpClient client, CancellationToken token = default);
}

