using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace SwebSocket;

public class ListenerBuilder
{
    internal ListenerOptions Options { get; set; }

    public ListenerBuilder() => Options = new ListenerOptions();

    public ListenerBuilder On(IPAddress address, ushort port)
    {
        Options.Address = address;
        Options.Port = port;
        return this;
    }

    public SslListenerBuilder UseSsl(bool useSsl = true)
    {
        Options.UseSsl = useSsl;
        return new SslListenerBuilder(Options);
    }

    public WebSocketListener Build() => new WebSocketListener(Options);
}

public class SslListenerBuilder : ListenerBuilder
{
    internal SslListenerBuilder(ListenerOptions options) => Options = options;

    public SslListenerBuilder WithServerCertificate(X509Certificate2? certificate)
    {
        Options.ServerCertificate = certificate;
        return this;
    }
}