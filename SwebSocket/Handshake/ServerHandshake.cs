using System.Text;

namespace SwebSocket;

internal class ServerHandshake : Handshake
{
    public ServerHandshake() { }

    public override async Task StartHandshake(Stream stream, CancellationToken token = default)
    {
        var requestText = await stream.ReadUntilAsync("\r\n\r\n", token);
        var request = HttpParser.Parse(requestText);

        string? secWebSocketKeyString;
        SecWebSocketKey? secWebSocketKey;

        if (
            request.MajorVersion != 1 || request.MinorVersion != 1
         || request.Method != "GET"
         || request.Headers.GetHttpHeader("Upgrade") != "websocket"
         || request.Headers.GetHttpHeader("Connection") != "Upgrade"
         || request.Headers.GetHttpHeader("Sec-WebSocket-Version") != "13"
         || (secWebSocketKeyString = request.Headers.GetHttpHeader("Sec-WebSocket-Key")) == null
         || (secWebSocketKey = SecWebSocketKey.FromHeader(secWebSocketKeyString)) == null
        ) throw new Exception("Invalid WebSocket handshake request");

        var response = Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Accept: " + secWebSocketKey!.AcceptHeaderValue + "\r\n" +
            "\r\n"
        );

        await stream.WriteAsync(response, token);
    }
}
