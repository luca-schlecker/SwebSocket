
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket;

internal class ConnectionFrameSocket
{
    public SocketState State { get; private set; }
    public event EventHandler? OnClosing;
    public event EventHandler? OnClosed;

    private FrameSocket frameSocket;
    private Handshake handshake;
    private CancellationTokenSource closeCts = new();
    private AsyncQueue<Frame> incoming = new();
    private AsyncQueue<Frame> outgoing = new();
    private TaskCompletionSource<bool> closeConfirmationReceived = new();
    private bool peerRequestedClose = false;

    internal ConnectionFrameSocket(FrameSocket frameSocket, Handshake handshake)
    {
        this.frameSocket = frameSocket;
        this.handshake = handshake;
        State = SocketState.Connecting;
        Task.Run(StartLifecycle);
    }

    public void Close()
    {
        Task.Run(async () =>
        {
            State = SocketState.Closing;
            OnClosing?.Invoke(this, EventArgs.Empty);
            await SendAsync(Frame.Close());
            await Task.WhenAny(closeConfirmationReceived.Task, Task.Delay(1000));
            closeCts.Cancel();
        });
    }
    public void Send(Frame frame) => outgoing.Enqueue(frame);
    public Frame Receive(CancellationToken token) => incoming.Dequeue(token);
    public async Task SendAsync(Frame frame) => await outgoing.EnqueueAsync(frame);
    public async Task<Frame> ReceiveAsync(CancellationToken token) => await incoming.DequeueAsync(token);

    private async Task StartLifecycle()
    {
        var token = closeCts.Token;
        try
        {
            await handshake.Perform(token);
            State = SocketState.Connected;

            var handleIncoming = HandleIncoming(token);
            var handleOutgoing = HandleOutgoing(token);

            await Task.WhenAny(handleIncoming, handleOutgoing);

            closeCts.Cancel();
            await Task.WhenAll(handleIncoming, handleOutgoing);

            if (peerRequestedClose)
                await frameSocket.SendAsync(Frame.Close());
        }
        catch { }
        finally
        {
            State = SocketState.Closed;
            frameSocket.Close();
            incoming.Close();
            outgoing.Close();
            OnClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task HandleOutgoing(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var frame = await outgoing.DequeueAsync(token);
            await frameSocket.SendAsync(frame);
        }
    }

    private async Task HandleIncoming(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var frame = await frameSocket.ReceiveAsync(token);
            if (IsUserFacingFrame(frame))
                await incoming.EnqueueAsync(frame);
            else
                await HandleInternalFrame(frame);
        }
    }

    private async Task HandleInternalFrame(Frame frame)
    {
        if (frame.OpCode == FrameOpCode.Close && State == SocketState.Closing)
            closeConfirmationReceived.TrySetResult(true);
        else if (frame.OpCode == FrameOpCode.Close && State != SocketState.Closing)
        {
            peerRequestedClose = true;
            OnClosing?.Invoke(this, EventArgs.Empty);
            closeCts.Cancel();
        }
        else if (frame.OpCode == FrameOpCode.Ping)
            await frameSocket.SendAsync(Frame.Pong(frame));
    }

    private static bool IsUserFacingFrame(Frame frame) => frame.OpCode switch
    {
        FrameOpCode.Text => true,
        FrameOpCode.Binary => true,
        FrameOpCode.Continuation => true,
        _ => false
    };
}