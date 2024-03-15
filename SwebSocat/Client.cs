using System.Net;
using SwebSocket;

namespace SwebSocat;

class Client
{
    public static async Task Start(Uri uri)
    {
        CancellationTokenSource cts = new();
        var token = cts.Token;

        using var ws = WebSocket.Connect(uri);
        ws.OnConnected += delegate { Console.WriteLine("[Connected]"); };
        ws.OnClosed += delegate
        {
            Console.WriteLine("[Closed]");
            cts.Cancel();
        };
        ws.OnMessage += (_, e) => Utility.PrintMessage(e);

        Console.CancelKeyPress += (_, e) =>
        {
            ws.Close();
            e.Cancel = true;
        };

        {
            var timeout = new CancellationTokenSource(2000);
            await ws.WaitForStatusAsync(SocketStatus.Connected, timeout.Token);
            if (ws.Status != SocketStatus.Connected)
            {
                Console.WriteLine("[Connection Timeouted]");
                return;
            }
        }

        while (ws.Status == SocketStatus.Connected)
        {
            var x = await Utility.ReadLineAsync(token);
            if (ws.Status != SocketStatus.Connected) break;
            else if (x == null) break;
            else ws.Send(new Message.Text(x));
        }
    }
}