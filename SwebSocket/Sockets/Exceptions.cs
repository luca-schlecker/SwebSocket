namespace SwebSocket;

public class SocketNotConnectedException : Exception
{
    public SocketNotConnectedException() : base("Socket is not connected") { }
}

public class MessagesInterleavedException : Exception
{
    public MessagesInterleavedException() : base("Messages are interleaved") { }
}

public class UnexpectedFrameException : Exception
{
    public UnexpectedFrameException() : base("Received an unexpected frame") { }
}