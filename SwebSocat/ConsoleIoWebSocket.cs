
using SwebSocket;

namespace SwebSocat;

static class ConsoleIoWebSocket
{
    public static async Task Handle(WebSocket ws)
    {
        CancellationTokenSource cts = new();
        ws.OnConnected += (_, _) => Console.WriteLine("[Connected]");
        ws.OnMessage += (_, m) => m.Print();
        ws.OnClosing += (_, _) =>
        {
            Console.WriteLine("[Closing]");
            cts.Cancel();
        };
        ws.OnClosed += (_, _) => Console.WriteLine("[Closed]");

        BackgroundMessagePoller.Poll(ws);

        while (true)
        {
            var line = await Utility.ReadLineAsync(cts.Token);
            if (line == null)
                break;
            ws.Send(new TextMessage(line));
        }

        ws.Close();

        while (ws.State != SocketState.Closed)
            await Task.Delay(50);
    }
}