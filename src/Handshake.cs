
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HttpMachine;
using HttpMessage = IHttpMachine.Model.HttpRequestResponse;

namespace SwebSocket;

internal abstract class Handshake
{
    protected Stream stream;

    public Handshake(Stream stream) => this.stream = stream;

    public abstract Task Perform(CancellationToken token = default);

    protected async Task<HttpMessage> ReadHttpMessage(CancellationToken token = default)
    {
        var buffer = new byte[1024];
        var bytesRead = 0;
        var canSafelyRead = GetSafelyReadableBytes(buffer, bytesRead);

        while (canSafelyRead > 0)
        {
            bytesRead += await stream.ReadAsync(buffer, bytesRead, canSafelyRead, token).ConfigureAwait(false);
            canSafelyRead = GetSafelyReadableBytes(buffer, bytesRead);
        }

        using var httpStream = new MemoryStream(buffer, 0, bytesRead);
        using var handler = new HttpParserDelegate();
        using var parser = new HttpCombinedParser(handler);

        if (parser.Execute(httpStream) == bytesRead)
            return handler.HttpRequestResponse;
        else throw new Exception("Failed to parse HTTP request");
    }

    private static int GetSafelyReadableBytes(byte[] buffer, int bytesRead)
    {
        if (bytesRead < 4) return 4 - bytesRead;
        var last = buffer.AsSpan(bytesRead - 4, 4);

        if (last[3] == '\r' && last[2] != '\n') return 3;
        if (last[3] == '\r' && last[2] == '\n' && last[1] != '\r') return 3;
        if (last[3] == '\r' && last[2] == '\n' && last[1] == '\r') return 1;

        if (last[3] == '\n' && last[2] != '\r') return 4;
        if (last[3] == '\n' && last[2] == '\r' && last[1] != '\n') return 2;
        if (last[3] == '\n' && last[2] == '\r' && last[1] == '\n' && last[0] != '\r') return 2;
        if (last[3] == '\n' && last[2] == '\r' && last[1] == '\n' && last[0] == '\r') return 0;

        return 4;
    }

    internal static string? GetHttpHeader(HttpMessage http, string key)
    {
        if (http == null) return null;
        key = key.ToUpper(); // HTTPMachine uses upper case keys
        if (!http.Headers.ContainsKey(key))
            return null;
        return string.Join(", ", http.Headers[key]);
    }
}

internal class ClientHandshake : Handshake
{
    private string host;
    private ushort port;
    private string path;
    SecWebSocketKey key = SecWebSocketKey.Random();

    public ClientHandshake(Stream stream, string host, ushort port, string path) : base(stream)
    {
        this.host = host;
        this.port = port;
        this.path = path;
    }

    public override async Task Perform(CancellationToken token = default)
    {
        var upgradeRequest = UpgradeRequest(key, $"{host}:{port}", path);
        await stream.WriteAsync(upgradeRequest, token).ConfigureAwait(false);
        var http = await ReadHttpMessage(token).ConfigureAwait(false);

        if (http!.MajorVersion != 1 || http.MinorVersion != 1)
            throw new Exception("Invalid Response HTTP Version");
        if (http.StatusCode != 101)
            throw new Exception("Invalid Response Status Code");
        if (GetHttpHeader(http, "Upgrade") != "websocket")
            throw new Exception("Invalid Response Upgrade Header");
        if (GetHttpHeader(http, "Connection")?.ToLower() != "upgrade")
            throw new Exception("Invalid Response Connection Header");
        if (GetHttpHeader(http, "Sec-WebSocket-Accept") != key.AcceptHeaderValue)
            throw new Exception("Invalid Response Sec-WebSocket-Accept Header");
        if (GetHttpHeader(http, "Sec-WebSocket-Protocol") != null)
            throw new Exception("Invalid Response Sec-WebSocket-Protocol");
        if (GetHttpHeader(http, "Sec-WebSocket-Extensions") != null)
            throw new Exception("Invalid Response Sec-WebSocket-Extensions");
    }

    private static byte[] UpgradeRequest(SecWebSocketKey key, string hostname, string location)
    {
        return System.Text.Encoding.ASCII.GetBytes(
            $"GET {location} HTTP/1.1\r\n" +
            $"Host: {hostname}\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            $"Sec-WebSocket-Key: {key.HeaderValue}\r\n" +
            "Sec-WebSocket-Version: 13\r\n" +
            "\r\n"
        );
    }
}

internal class ServerHandshake : Handshake
{
    public ServerHandshake(Stream stream) : base(stream) { }

    public override async Task Perform(CancellationToken token = default)
    {
        var http = await ReadHttpMessage(token).ConfigureAwait(false);

        string? secWebSocketKeyString;
        SecWebSocketKey? secWebSocketKey;

        if (http!.MajorVersion != 1 || http.MinorVersion != 1
         || http.Method != "GET"
         || GetHttpHeader(http, "Upgrade")?.ToLower() != "websocket"
         || GetHttpHeader(http, "Connection")?.ToLower() != "upgrade"
         || GetHttpHeader(http, "Sec-WebSocket-Version") != "13"
         || (secWebSocketKeyString = GetHttpHeader(http, "Sec-WebSocket-Key")) == null
         || (secWebSocketKey = SecWebSocketKey.FromHeader(secWebSocketKeyString)) == null
        ) throw new Exception("Invalid WebSocket handshake request");

        var response = UpgradeResponse(secWebSocketKey);
        await stream.WriteAsync(response, token).ConfigureAwait(false);
    }

    private static byte[] UpgradeResponse(SecWebSocketKey key)
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Accept: " + key!.AcceptHeaderValue + "\r\n" +
            "\r\n"
        );
    }
}