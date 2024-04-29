
using SwebSocket;

namespace SwebSocat;

static class Server
{
    public static async Task Start(Listener listener)
    {
        WebSocket? ws = null;
        Console.CancelKeyPress += (_, e) =>
        {
            if (ws is WebSocket w)
                ws.Close();
            else
                Environment.Exit(0);

            e.Cancel = true;
        };

        while (true)
        {
            ws = listener.Accept();
            Console.WriteLine("[Client Connected]");

            await ConsoleIoWebSocket.Handle(ws);

            ws = null;
        }
    }
}