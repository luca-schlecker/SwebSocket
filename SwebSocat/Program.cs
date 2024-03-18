namespace SwebSocat;

using System.Net;
using System.Security.Cryptography.X509Certificates;
using CommandLine;
using CommandLine.Text;
using SwebSocket;

partial class Program
{
    private static ParserResult<Options>? result;

    public static async Task Main(string[] args)
    {
        result = Parser.Default.ParseArguments<Options>(args);
        await result.WithParsedAsync(HandleOptions);
    }

    private static async Task HandleOptions(Options o)
    {
        if (o.Listen)
        {
            if (o.Port.HasValue && o.Uri is null)
            {
                try
                {
                    var listener = new ListenerBuilder()
                        .On(IPAddress.Any, o.Port.Value)
                        .UseSsl(o.UseSsl)
                        .WithServerCertificate(LoadPemCertificate(o.PemCertificatePath, o.PemKeyPath))
                        .Build();

                    await Server.Start(listener);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to start server:\n{e.Message}");
                }
            }
            else if (o.Uri != null && !o.Port.HasValue)
            {
                if (o.Uri.Scheme != "ws" && o.Uri.Scheme != "wss")
                {
                    Console.WriteLine("Invalid URI scheme");
                    return;
                }

                if (o.Uri.HostNameType != UriHostNameType.IPv4 && o.Uri.HostNameType != UriHostNameType.IPv6)
                {
                    Console.WriteLine("Cannot listen on hostnames");
                    return;
                }

                o.UseSsl |= o.Uri.Scheme == "wss";

                try
                {
                    var listener = new ListenerBuilder()
                        .On(IPAddress.Parse(o.Uri.Host), (ushort)o.Uri.Port)
                        .UseSsl(o.UseSsl)
                        .WithServerCertificate(LoadPemCertificate(o.PemCertificatePath, o.PemKeyPath))
                        .Build();

                    await Server.Start(listener);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to start server:\n{e.Message}");
                }
            }
            else if (o.Uri != null && o.Port.HasValue)
                Console.WriteLine("Cannot specify both a port and a URI");
            else
            {
                Console.WriteLine("You have to specify either a port or a URI");
                PrintHelp();
            }
        }
        else
        {
            if (o.Uri is null)
            {
                Console.WriteLine("You have to specify a URI");
                PrintHelp();
                return;
            }
            else if (o.Port.HasValue)
            {
                Console.WriteLine("Cannot specify both a port and a URI");
                return;
            }

            if (o.Uri.Scheme != "ws" && o.Uri.Scheme != "wss")
            {
                Console.WriteLine("Invalid URI scheme");
                return;
            }

            var ws = new ConnectionBuilder()
                .To(o.Uri)
                .UseSsl(o.Uri.Scheme == "wss")
                .WithCaCertificate(LoadCertificate(o.CaCertificatePath))
                .ValidateAuthority(o.ValidateAuthority ?? o.Uri.Host)
                .Build();

            await Client.Start(ws);
        }
    }

    private static X509Certificate2? LoadCertificate(string? path)
    {
        if (path is null) return null;
        try
        {
            return new X509Certificate2(path);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to load certificate: {e.Message}", e);
        }
    }

    private static X509Certificate2? LoadPemCertificate(string? certPath, string? keyPath)
    {
        if (certPath is null && keyPath is null) return null;
        else if (!(certPath is not null && keyPath is not null))
            throw new ArgumentException("You have to specify both a certificate and a key");

        try
        {
            return X509Certificate2.CreateFromPemFile(certPath, keyPath);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to load certificate: {e.Message}", e);
        }
    }

    private static void PrintHelp()
    {
        string helpText = HelpText.AutoBuild(result, h => h, e => e);
        Console.WriteLine(helpText);
    }
}