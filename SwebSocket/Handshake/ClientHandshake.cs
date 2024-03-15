using System.Net.Sockets;
using System.Text;

namespace SwebSocket;

internal class ClientHandshake : Handshake
{
    private string host;
    private ushort port;
    private string path;

    public ClientHandshake(string host, ushort port, string path)
    {
        this.host = host;
        this.port = port;
        this.path = path;
    }

    public override async Task StartHandshake(TcpClient client, CancellationToken token = default)
    {
        var key = SecWebSocketKey.Random();
        var stream = client.GetStream();

        var upgradeRequest = UpgradeRequest(key, $"{host}:{port}", path);
        await stream.WriteAsync(upgradeRequest, token);
        var request = await stream.ReadUntilAsync("\r\n\r\n", token);
        var httpResponse = HttpParser.Parse(request);

        if (httpResponse.MajorVersion != 1 || httpResponse.MinorVersion != 1)
            throw new Exception("Invalid Response HTTP Version");
        if (httpResponse.StatusCode != 101)
            throw new Exception("Invalid Response Status Code");
        if (httpResponse.Headers.GetHttpHeader("Upgrade") != "websocket")
            throw new Exception("Invalid Response Upgrade Header");
        if (httpResponse.Headers.GetHttpHeader("Connection") != "Upgrade")
            throw new Exception("Invalid Response Connection Header");
        if (httpResponse.Headers.GetHttpHeader("Sec-WebSocket-Accept") != key.AcceptHeaderValue)
            throw new Exception("Invalid Response Sec-WebSocket-Accept Header");
        if (httpResponse.Headers.GetHttpHeader("Sec-WebSocket-Protocol") != null)
            throw new Exception("Invalid Response Sec-WebSocket-Protocol");
        if (httpResponse.Headers.GetHttpHeader("Sec-WebSocket-Extensions") != null)
            throw new Exception("Invalid Response Sec-WebSocket-Extensions");
    }

    private static byte[] UpgradeRequest(SecWebSocketKey key, string hostname, string location)
    {
        return Encoding.ASCII.GetBytes(
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
