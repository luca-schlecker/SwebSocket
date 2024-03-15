using System.Net.Sockets;

namespace SwebSocket;

internal class FrameSocket : Socket<Frame>, IDisposable
{
    private TcpClient client;
    private CancellationTokenSource cts = new();
    private Task poller;
    private SemaphoreSlim sendLock = new(1);
    private readonly MaskingBehavior masking;

    public FrameSocket(TcpClient client, MaskingBehavior masking)
    {
        this.client = client;
        this.masking = masking;
        Status = client.Connected ? SocketStatus.Connected : SocketStatus.Closed;
        poller = Status == SocketStatus.Connected
            ? Task.Run(() => StartPolling(cts.Token), cts.Token)
            : Task.CompletedTask;
    }

    public override void Close()
    {
        if (TestThenSetStatus(SocketStatus.Connected, SocketStatus.Closing))
        {
            cts.Cancel();
            poller.Wait();
            sendLock.Wait();
            client.Close();
            sendLock.Release();
            Status = SocketStatus.Closed;
            EmitClosed();
        }
    }

    public override async Task CloseAsync()
    {
        if (TestThenSetStatus(SocketStatus.Connected, SocketStatus.Closing))
        {
            await cts.CancelAsync();
            await poller;
            await sendLock.WaitAsync();
            client.Close();
            sendLock.Release();
            Status = SocketStatus.Closed;
            EmitClosed();
        }
    }

    public override void Send(Frame frame) => SendMany([frame]);
    public override async Task SendAsync(Frame frame) => await SendManyAsync([frame]);

    public void SendMany(IEnumerable<Frame> frames)
    {
        if (Status != SocketStatus.Connected)
            throw new SocketNotConnectedException();

        sendLock.Wait();

        if (Status != SocketStatus.Connected)
        {
            sendLock.Release();
            throw new SocketNotConnectedException();
        }

        try
        {
            var stream = client.GetStream();
            var frameStream = new FrameStream(stream);
            foreach (var frame in frames)
            {
                if (masking == MaskingBehavior.MaskOutgoing) frame.Mask();
                frameStream.WriteFrame(frame);
            }
        }
        catch { }
        finally { sendLock.Release(); }
    }

    public async Task SendManyAsync(IEnumerable<Frame> frames, CancellationToken token = default)
    {
        if (Status != SocketStatus.Connected)
            throw new InvalidOperationException("Socket is not connected");

        await sendLock.WaitAsync();

        if (Status != SocketStatus.Connected)
        {
            sendLock.Release();
            throw new SocketNotConnectedException();
        }

        try
        {
            var stream = client.GetStream();
            var frameStream = new FrameStream(stream);
            foreach (var frame in frames)
            {
                if (masking == MaskingBehavior.MaskOutgoing) frame.Mask();
                await frameStream.WriteFrameAsync(frame, token);
            }
        }
        catch { }
        finally { sendLock.Release(); }
    }

    private async Task StartPolling(CancellationToken token = default)
    {
        try
        {
            var stream = client.GetStream();
            var frameStream = new FrameStream(stream);

            while (!token.IsCancellationRequested)
            {
                var frame = await frameStream.ReadFrameAsync(token);
                if (masking == MaskingBehavior.UnmaskIncoming) frame.Unmask();
                EmitMessage(frame);
            }
        }
        catch (OperationCanceledException) { }
        catch
        {
            _ = Task.Run(Close);
        }
    }

    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Close();
                cts.Dispose();
                sendLock.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}