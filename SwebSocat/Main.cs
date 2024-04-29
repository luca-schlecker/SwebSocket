
using System.Net;
using System.Security.Cryptography.X509Certificates;
using CommandLine;
using SwebSocat;
using SwebSocket;
using static Crayon.Output;

var result = Parser.Default.ParseArguments<Options>(args);
await result.WithParsedAsync(async options =>
{
    if (options.Listen)
    {
        var ip = options.Uri?.Host ?? "0.0.0.0";
        var port = options.Port ?? options.Uri?.Port ?? 8080;
        var ssl = SslOptions.NoSsl;
        if (options.PemKeyPath != null || options.PemCertificatePath != null)
        {
            if (options.PemKeyPath == null || options.PemCertificatePath == null)
            {
                Console.WriteLine("Both --pem-cert and --pem-key must be provided");
                return;
            }

            ssl = SslOptions.ForServer(LoadPemCertificate(options.PemCertificatePath, options.PemKeyPath));
        }

        var listenAddr = Magenta().Underline($"{(ssl == SslOptions.NoSsl ? "ws" : "wss")}://{ip}:{port}/");
        Console.WriteLine(Bold().Blue($"[Listening on {listenAddr}]"));

        var listener = new Listener(IPAddress.Parse(ip), (ushort)port);
        await Server.Start(listener);
    }
    else
    {
        if (options.Uri == null)
        {
            Console.WriteLine("An address must be provided");
            return;
        }

        var uri = options.Uri;
        var authority = options.ValidateAuthority ?? uri.Authority;
        var ssl = options.Uri.Scheme == "wss"
            ? SslOptions.ForClient(authority)
            : SslOptions.NoSsl;
        if (options.CaCertificatePath != null)
            ssl = SslOptions.ForClient(
                authority,
                new X509Certificate2Collection() {
                    LoadCertificate(options.CaCertificatePath)
                }
            );

        var ws = WebSocket
            .Connect()
            .To(uri)
            .UseSsl(ssl)
            .Build();
        await Client.Start(ws);
    }
});

X509Certificate2 LoadCertificate(string path)
{
    try
    {
        return new X509Certificate2(path);
    }
    catch (Exception e)
    {
        throw new Exception($"Failed to load certificate: {e.Message}", e);
    }
}

X509Certificate2 LoadPemCertificate(string certPath, string keyPath)
{
    try
    {
        return X509Certificate2.CreateFromPemFile(certPath, keyPath);
    }
    catch (Exception e)
    {
        throw new Exception($"Failed to load certificate: {e.Message}", e);
    }
}