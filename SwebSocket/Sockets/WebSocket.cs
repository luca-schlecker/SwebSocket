
using System.Net;
using System.Net.Sockets;

namespace SwebSocket;

public class WebSocket : Socket<Message>, IDisposable
{
    private FrameSocket? frameSocket;
    private MessageBuilder messageBuilder = new();
    private IMessageSplitter messageSplitter;
    private CancellationTokenSource cancelHandshake = new();

    internal WebSocket(TcpClient client, Handshake handshake, MaskingBehavior masking)
    {
        Status = SocketStatus.Connecting;
        messageSplitter = new DefaultMessageSplitter();
        Task.Run(() => InitiateHandshake(client, handshake, masking));
    }

    private async void InitiateHandshake(TcpClient client, Handshake handshake, MaskingBehavior masking)
    {
        try
        {
            await handshake.StartHandshake(client, cancelHandshake.Token);
            var status = StartTestThenSetStatus();
            if (Status == SocketStatus.Connecting)
            {
                frameSocket = new FrameSocket(client, masking);
                frameSocket.OnMessage += FrameReceived;
                frameSocket.OnClosed += HandleBrokenPipe;
                EndTestThenSetStatus(SocketStatus.Connected);
                EmitConnected();
            }
            else EndTestThenSetStatus(status);
        }
        catch (OperationCanceledException)
        {
            client.Close();
        }
        catch
        {
            client.Close();
            Status = SocketStatus.Closed;
            EmitClosed();
        }
    }

    public override void Close()
    {
        var status = StartTestThenSetStatus();
        if (status == SocketStatus.Connecting)
        {
            EndTestThenSetStatus(SocketStatus.Closed);

            cancelHandshake.Cancel();
            EmitClosed();
        }
        else if (status == SocketStatus.Connected)
        {
            EndTestThenSetStatus(SocketStatus.Closing);

            frameSocket!.Send(Frame.Close());
            var source = new CancellationTokenSource(500);
            WaitForStatus(SocketStatus.Closed, source.Token);
            if (TestThenSetStatus(SocketStatus.Closing, SocketStatus.Closed))
            {
                frameSocket!.Close();
                EmitClosed();
            }
        }
    }

    public override async Task CloseAsync()
    {
        var status = StartTestThenSetStatus();
        if (status == SocketStatus.Connecting)
        {
            EndTestThenSetStatus(SocketStatus.Closed);

            await cancelHandshake.CancelAsync();
            EmitClosed();
        }
        else if (status == SocketStatus.Connected)
        {
            EndTestThenSetStatus(SocketStatus.Closing);

            await frameSocket!.SendAsync(Frame.Close());
            var source = new CancellationTokenSource(500);
            await WaitForStatusAsync(SocketStatus.Closed, source.Token);
            if (TestThenSetStatus(SocketStatus.Closing, SocketStatus.Closed))
            {
                await frameSocket!.CloseAsync();
                EmitClosed();
            }
        }
    }

    public override void Send(Message message)
    {
        if (Status != SocketStatus.Connected)
            throw new SocketNotConnectedException();

        var frames = message.GetFrames(messageSplitter);
        frameSocket!.SendMany(frames);
    }

    public override async Task SendAsync(Message message)
    {
        if (Status != SocketStatus.Connected)
            throw new SocketNotConnectedException();

        var frames = message.GetFrames(messageSplitter);
        await frameSocket!.SendManyAsync(frames);
    }

    private void FrameReceived(object? sender, Frame frame)
    {
        switch (frame.OpCode)
        {
            case FrameOpCode.Continuation:
            case FrameOpCode.Text:
            case FrameOpCode.Binary:
                HandleDataFrame(frame);
                break;
            case FrameOpCode.Ping:
                HandlePingFrame(frame);
                break;
            case FrameOpCode.Pong:
                HandlePongFrame(frame);
                break;
            case FrameOpCode.Close:
                HandleCloseFrame(frame);
                break;
            default:
                throw new Exception("Invalid Frame OpCode");
        }
    }

    private void HandleCloseFrame(Frame frame)
    {
        if (TestThenSetStatus(SocketStatus.Closing, SocketStatus.Closed))
        {
            frameSocket!.Close();
            EmitClosed();
        }
        else
        {
            Task.Run(InitiateClose);
        }
    }

    private void HandlePingFrame(Frame frame)
    {
        Task.Run(() => frameSocket!.Send(Frame.Pong(frame)));
    }

    private void HandlePongFrame(Frame frame) { }

    private void HandleDataFrame(Frame frame)
    {
        try
        {
            var message = messageBuilder.Append(frame);
            if (message != null) EmitMessage(message);
        }
        catch (UnexpectedFrameException)
        {
            _ = Task.Run(Close);
        }
        catch (MessagesInterleavedException)
        {
            _ = Task.Run(Close);
        }
    }

    private void InitiateClose()
    {
        if (TestThenSetStatus(SocketStatus.Connected, SocketStatus.Closing))
        {
            frameSocket!.Send(Frame.Close());
            Status = SocketStatus.Closed;
            frameSocket!.Close();
            EmitClosed();
        }
    }

    private void HandleBrokenPipe(object? sender, EventArgs e)
    {
        if (TestThenSetStatus(
            s => s == SocketStatus.Connected || s == SocketStatus.Connecting,
            SocketStatus.Closed
        )) EmitClosed();
    }

    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Close();
                frameSocket?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static WebSocket Connect(Uri uri)
    {
        if (uri.Scheme != "ws" && uri.Scheme != "wss")
            throw new ArgumentException("Invalid URI Scheme");

        var client = new TcpClient();
        client.Connect(uri.Host, uri.Port);
        return new WebSocket(
            client,
            new ClientHandshake(uri.Host, (ushort)uri.Port, uri.AbsolutePath),
            MaskingBehavior.MaskOutgoing
        );
    }

    // public static WebSocket Connect(IPEndPoint remote, string path = "/")
    //     => Connect(remote.Address, (ushort)remote.Port, path);

    // public static WebSocket Connect(IPAddress address, ushort port, string path = "/")
    // {
    //     var client = new TcpClient();
    //     client.Connect(address, port);
    //     return new WebSocket(
    //         client,
    //         new ClientHandshake(address.ToString(), port, path),
    //         MaskingBehavior.MaskOutgoing
    //     );
    // }
}