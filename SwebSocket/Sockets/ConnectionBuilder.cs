using System;
using System.Security.Cryptography.X509Certificates;

namespace SwebSocket
{
    public class ConnectionBuilder
    {
        internal ConnectionOptions Options { get; set; }

        public ConnectionBuilder() => Options = new ConnectionOptions();

        /// <remarks>
        /// Providing a 'wss' Uri will NOT automatically enable SSL. Use <see cref="UseSsl"/> instead;
        /// </remarks>
        public ConnectionBuilder To(Uri uri)
        {
            if (uri.Scheme != "wss" && uri.Scheme != "ws")
                throw new ArgumentException("Invalid URI Scheme");

            Options.Host = uri.Host;
            Options.Port = (ushort)uri.Port;
            Options.Path = uri.AbsolutePath;
            return this;
        }

        public SslConnectionBuilder UseSsl(bool useSsl = true)
        {
            Options.UseSSL = useSsl;
            return new SslConnectionBuilder(Options);
        }

        public WebSocket Build() => new WebSocket(Options);
    }

    public class SslConnectionBuilder : ConnectionBuilder
    {
        internal SslConnectionBuilder(ConnectionOptions options) => Options = options;

        public SslConnectionBuilder WithCaCertificate(X509Certificate2? certificate)
        {
            Options.CaCertificate = certificate;
            return this;
        }

        public SslConnectionBuilder ValidateAuthority(string? authority)
        {
            Options.ValidatedAuthority = authority;
            return this;
        }
    }
}