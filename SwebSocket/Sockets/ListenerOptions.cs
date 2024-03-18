using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace SwebSocket;

internal class ListenerOptions
{
    public IPAddress? Address { get; set; }
    public ushort? Port { get; set; }
    public bool UseSsl { get; set; } = false;
    public X509Certificate2? ServerCertificate { get; set; }
}