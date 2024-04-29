
using SwebSocket;
using static Crayon.Output;

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
            {
                Console.WriteLine(Bold().Blue("[Exiting]"));
                Environment.Exit(0);
            }

            e.Cancel = true;
        };

        while (true)
        {
            ws = listener.Accept();
            await ConsoleIoWebSocket.Handle(ws);
            ws = null;
        }
    }
}