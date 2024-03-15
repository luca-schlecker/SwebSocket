namespace SwebSocket;

internal abstract class Handshake
{
    public abstract Task StartHandshake(Stream stream, CancellationToken token = default);
}

