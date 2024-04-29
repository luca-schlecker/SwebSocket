
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket;

public class BlockingMessagePoller
{
    private WebSocket ws;
    public BlockingMessagePoller(WebSocket ws) => this.ws = ws;

    public void Poll(CancellationToken token = default)
    {
        try
        {
            while (ShouldPoll())
            {
                token.ThrowIfCancellationRequested();
                var msg = ws.Receive(token);
                ws.EmitMessage(msg);
            }
        }
        catch { }
    }

    public async Task PollAsync(CancellationToken token = default)
    {
        try
        {
            while (ShouldPoll())
            {
                token.ThrowIfCancellationRequested();
                var msg = await ws.ReceiveAsync(token);
                ws.EmitMessage(msg);
            }
        }
        catch { }
    }

    private bool ShouldPoll() => ws.State == SocketState.Connected || ws.State == SocketState.Connecting;
}

public static class BackgroundMessagePoller
{
    public static void Poll(WebSocket ws) => Task.Run(async () =>
    {
        var poller = new BlockingMessagePoller(ws);
        await poller.PollAsync();
    });
}