using System.Security.Cryptography.X509Certificates;

namespace SwebSocket;

internal class ConnectionOptions
{
    public string? Host { get; set; }
    public ushort? Port { get; set; }
    public string? Path { get; set; }
    public bool UseSSL { get; set; } = false;
    public string? ValidatedAuthority { get; set; }
    public X509Certificate2? CaCertificate { get; set; }
}