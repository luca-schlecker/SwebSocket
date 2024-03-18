namespace SwebSocat;

using System.Net;
using System.Security.Cryptography.X509Certificates;
using CommandLine;
using CommandLine.Text;

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
        if (o.Uri == null && o.Port.HasValue)
        {
            X509Certificate? cert = null;
            if (o.CertificatePath != null)
            {
                try
                {
                    cert = new X509Certificate(o.CertificatePath);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Failed to load certificate: " + e.Message);
                    return;
                }
            }
            await Server.Start(o.Port.Value, cert);
        }
        else if (o.Uri != null && !o.Port.HasValue)
            await Client.Start(o.Uri);
        else
            PrintHelp();
    }

    private static void PrintHelp()
    {
        string helpText = HelpText.AutoBuild(result, h => h, e => e);
        Console.WriteLine(helpText);
    }
}