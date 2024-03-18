using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace SwebSocket;

public class WebSocket : Socket<Message>, IDisposable
{
    private FrameSocket? frameSocket;
    private MessageBuilder messageBuilder = new();
    private IMessageSplitter messageSplitter;
    private CancellationTokenSource cancelHandshake = new();
    private TcpClient client;

    internal WebSocket(TcpClient client, Stream stream, Handshake handshake, MaskingBehavior masking)
    {
        this.client = client;
        Status = SocketStatus.Connecting;
        messageSplitter = new DefaultMessageSplitter();
        Task.Run(() => InitiateHandshake(stream, handshake, masking));
    }

    private async void InitiateHandshake(Stream stream, Handshake handshake, MaskingBehavior masking)
    {
        try
        {
            await handshake.StartHandshake(stream, cancelHandshake.Token);
            var status = StartTestThenSetStatus();
            if (Status == SocketStatus.Connecting)
            {
                frameSocket = new FrameSocket(stream, masking);
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
                client.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    internal WebSocket(ConnectionOptions options)
    {
        var client = new TcpClient();
        client.Connect(options.Host!, options.Port!.Value);

        var stream = GetStream(client, options);
        var handshake = new ClientHandshake(
            options.Host!,
            options.Port.Value,
            options.Path!
        );
        var masking = MaskingBehavior.MaskOutgoing;

        this.client = client;
        Status = SocketStatus.Connecting;
        messageSplitter = new DefaultMessageSplitter();
        Task.Run(() => InitiateHandshake(stream, handshake, masking));
    }

    private Stream GetStream(TcpClient client, ConnectionOptions options)
    {
        if (options.UseSSL)
        {
            var sslStream = new SslStream(client.GetStream(), false);
            var authOptions = new SslClientAuthenticationOptions();

            if (options.ValidatedAuthority is not null)
                authOptions.TargetHost = options.ValidatedAuthority;

            if (options.CaCertificate is not null)
            {
                authOptions.CertificateChainPolicy = new X509ChainPolicy
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    TrustMode = X509ChainTrustMode.CustomRootTrust
                };
                authOptions.CertificateChainPolicy.CustomTrustStore.Add(options.CaCertificate);
            }

            sslStream.AuthenticateAsClient(authOptions);
            return sslStream;
        }
        else return client.GetStream();
    }
}