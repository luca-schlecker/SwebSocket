
using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SwebSocket;

public delegate Stream SslStreamFactory(Stream stream);

public class SslOptions
{
    internal SslStreamFactory SslStreamFactory { get; private set; }

    internal SslOptions(SslStreamFactory sslStreamFactory) => SslStreamFactory = sslStreamFactory;

    public static readonly SslOptions NoSsl = new SslOptions(stream => stream);

    public static SslOptions ForServer(X509Certificate2 identity) => new SslOptions(stream =>
    {
        var sslStream = new SslStream(stream, true);
        sslStream.AuthenticateAsServer(identity);
        return sslStream;
    });

    public static SslOptions ForClient(string targetHost, RemoteCertificateValidationCallback callback) => new SslOptions(stream =>
    {
        var sslStream = new SslStream(stream, true, callback);
        sslStream.AuthenticateAsClient(targetHost);
        return sslStream;
    });

    public static SslOptions ForClient(string targetHost) => new SslOptions(stream =>
    {
        var sslStream = new SslStream(stream, true);
        sslStream.AuthenticateAsClient(targetHost);
        return sslStream;
    });

    public static SslOptions ForClient(string targetHost, X509Certificate2Collection caCertificates) => new SslOptions(stream =>
    {
        var remoteCallback = RemoteCertificateValidationCallbackWithCaCertificates(caCertificates);
        var sslStream = new SslStream(stream, true, remoteCallback);
        sslStream.AuthenticateAsClient(targetHost);
        return sslStream;
    });

    private static RemoteCertificateValidationCallback RemoteCertificateValidationCallbackWithCaCertificates(X509Certificate2Collection caCertificates) => (sender, certificate, chain, sslPolicyErrors) =>
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
            return false;

        chain.ChainPolicy.ExtraStore.AddRange(caCertificates);

        if (chain.Build(new X509Certificate2(certificate)))
            return true;

        if (chain.ChainStatus.Any(status => status.Status != X509ChainStatusFlags.UntrustedRoot))
            return false;


        foreach (var element in chain.ChainElements)
        {
            foreach (var status in element.ChainElementStatus)
            {
                if (status.Status == X509ChainStatusFlags.UntrustedRoot)
                {
                    var skip = false;

                    foreach (var ca in caCertificates)
                        skip |= ca.RawData.SequenceEqual(element.Certificate.RawData);

                    if (skip)
                        continue;
                }

                return false;
            }
        }

        return true;
    };
}