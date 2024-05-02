
using System.Collections.Concurrent;
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
    private static ConcurrentDictionary<WebSocket, CancellationTokenSource> pollers = new();

    public static void Poll(WebSocket ws, CancellationToken token = default) => Task.Run(async () =>
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var linked = cts.Token;

        if (pollers.TryAdd(ws, cts))
        {
            await PollContinuously(ws, linked).ConfigureAwait(false);
            if (pollers.TryRemove(ws, out cts))
                cts.Dispose();
        }
    });

    private static async Task PollContinuously(WebSocket ws, CancellationToken token)
    {
        try
        {
            while (ws.State == SocketState.Connected || ws.State == SocketState.Connecting)
            {
                token.ThrowIfCancellationRequested();
                var msg = await ws.ReceiveAsync(token).ConfigureAwait(false);
                ws.EmitMessage(msg);
            }
        }
        catch { }
    }

    public static bool TryStopPolling(WebSocket ws)
    {
        var removed = pollers.TryRemove(ws, out var cts);
        if (removed) { cts.Cancel(); cts.Dispose(); }
        return removed;
    }
}