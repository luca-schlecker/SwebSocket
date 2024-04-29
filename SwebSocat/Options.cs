
using CommandLine;

namespace SwebSocat;

class Options
{
    [Option('l', "listen", HelpText = "Start a server", Required = false, Default = false, SetName = "listen")]
    public bool Listen { get; set; }
    [Option('p', "port", HelpText = "The port to listen on", Required = false)]
    public ushort? Port { get; set; }
    [Option("ca", HelpText = "The path to the ca certificate file", Required = false, SetName = "connect")]
    public string? CaCertificatePath { get; set; }
    [Option("pem-cert", HelpText = "The path to the pem certificate file", Required = false, SetName = "listen")]
    public string? PemCertificatePath { get; set; }
    [Option("pem-key", HelpText = "The path to the pem key file", Required = false, SetName = "listen")]
    public string? PemKeyPath { get; set; }
    [Option("validate-authority", HelpText = "Validate the certificate authority", Required = false, SetName = "connect")]
    public string? ValidateAuthority { get; set; }

    [Value(0, MetaName = "address", Required = false)]
    public Uri? Uri { get; set; }
}