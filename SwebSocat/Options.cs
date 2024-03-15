using CommandLine;

namespace SwebSocat;

class Options
{
    [Value(0, HelpText = "The URI to connect to", MetaName = "address", Required = false)]
    public Uri? Uri { get; set; }
    [Option('l', "listen", HelpText = "Start a server", Required = false)]
    public ushort? Port { get; set; }
}