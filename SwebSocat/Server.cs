using SwebSocket;

namespace SwebSocat;

class Server
{
    public static async Task Start(WebSocketListener pListener)
    {
        bool running = true;
        using var listener = pListener;
        var protocol = listener.UseSsl ? "wss" : "ws";
        Console.WriteLine($"Listening on {protocol}://{listener.Address}:{listener.Port}/");

        using var cts = new ResettableCancellationTokenSource();

        WebSocket? ws = null;

        Console.CancelKeyPress += (_, e) =>
        {
            if (ws is WebSocket s)
                Task.Run(() => s.Close());
            else
            {
                running = false;
                cts.CancelAndReset();
            }
            e.Cancel = true;
        };

        while (running)
        {
            var token = cts.Token;

            try { ws = await listener.AcceptAsync(token); }
            catch { continue; }

            ws.OnConnected += delegate { Console.WriteLine("[Client Connected]"); };
            ws.OnClosed += delegate
            {
                Console.WriteLine("[Client Closed]");
                cts.CancelAndReset();
            };
            ws.OnMessage += (_, e) => Utility.PrintMessage(e);

            await ws.WaitForStatusAsync(SocketStatus.Connected, token);
            if (ws.Status != SocketStatus.Connected)
                Console.WriteLine("[Connection Failed]");

            while (ws.Status == SocketStatus.Connected)
            {
                string? input = await Utility.ReadLineAsync(token);
                if (ws.Status != SocketStatus.Connected) break;
                if (input == null) break;
                ws.Send(new Message.Text(input.Trim()));
            }

            ws.Dispose();
            ws = null;
        }
    }
}
