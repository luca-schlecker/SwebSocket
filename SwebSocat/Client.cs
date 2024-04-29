

using SwebSocket;

namespace SwebSocat;

class Client
{
    public static async Task Start(WebSocket ws)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            ws.Close();
            e.Cancel = true;
        };

        await ConsoleIoWebSocket.Handle(ws);
    }
}