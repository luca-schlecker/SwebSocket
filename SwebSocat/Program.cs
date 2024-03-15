namespace SwebSocat;

using System.Net;
using CommandLine;
using CommandLine.Text;

partial class Program
{
    private static ParserResult<Options>? result;

    public static async Task Main(string[] args)
    {
        // await Server.Start(8080);
        result = Parser.Default.ParseArguments<Options>(args);
        await result.WithParsedAsync(HandleOptions);
    }

    private static async Task HandleOptions(Options o)
    {
        if (o.Uri == null && o.Port.HasValue)
            await Server.Start(o.Port.Value);
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