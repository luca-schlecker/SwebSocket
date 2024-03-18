using System.Security.Cryptography.X509Certificates;

namespace SwebSocket;

public class ServerSSLOptions
{
    public bool UseSSL { get; private set; } = false;
    public X509Certificate? Certificate { get; private set; } = null;

    public static ServerSSLOptions NoSSL()
    {
        return new ServerSSLOptions { UseSSL = false };
    }

    public static ServerSSLOptions WithCertificate(X509Certificate certificate)
    {
        return new ServerSSLOptions { UseSSL = true, Certificate = certificate };
    }
}