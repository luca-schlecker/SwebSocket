
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket;

internal class ConnectionFrameSocket
{
    public SocketState State { get; private set; }
    public event EventHandler? OnClosing;
    public event EventHandler? OnClosed;
    public event EventHandler? OnConnected;

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

    public void Close() => closeCts.Cancel();
    public void Send(Frame frame) => outgoing.Enqueue(frame);
    public void SendRange(IEnumerable<Frame> frames) => outgoing.EnqueueRange(frames);
    public Frame Receive(CancellationToken token) => incoming.Dequeue(token);
    public async Task SendAsync(Frame frame) => await outgoing.EnqueueAsync(frame).ConfigureAwait(false);
    public async Task SendRangeAsync(IEnumerable<Frame> frames) => await outgoing.EnqueueRangeAsync(frames).ConfigureAwait(false);
    public async Task<Frame> ReceiveAsync(CancellationToken token) => await incoming.DequeueAsync(token).ConfigureAwait(false);

    private async Task StartLifecycle()
    {
        var token = closeCts.Token;

        try
        {
            await handshake.Perform(token).ConfigureAwait(false);
            State = SocketState.Connected;

            OnConnected?.Invoke(this, EventArgs.Empty);

            _ = Task.Run(PingWorker);

            var handleIncoming = HandleIncoming(token);
            var handleOutgoing = HandleOutgoing(token);

            await Task.WhenAny(handleIncoming, handleOutgoing).ConfigureAwait(false);
            closeCts.Cancel();
            await Task.WhenAll(handleIncoming, handleOutgoing).ConfigureAwait(false);
        }
        catch { }

        var prevState = State;
        State = SocketState.Closing;
        OnClosing?.Invoke(this, EventArgs.Empty);

        try
        {
            if (peerRequestedClose)
                await frameSocket.SendAsync(Frame.Close()).ConfigureAwait(false);
            else if (prevState == SocketState.Connected)
            {
                await frameSocket.SendAsync(Frame.Close()).ConfigureAwait(false);
                var pollCts = new CancellationTokenSource(1000);
                await ReadCloseFrame(pollCts.Token).ConfigureAwait(false);
            }
        }
        catch { }

        State = SocketState.Closed;
        frameSocket.Close();
        incoming.Close();
        outgoing.Close();
        OnClosed?.Invoke(this, EventArgs.Empty);
    }

    private async Task PingWorker()
    {
        while (State == SocketState.Connected)
        {
            await Task.Delay(5000).ConfigureAwait(false);
            await SendAsync(Frame.Ping()).ConfigureAwait(false);
        }
    }

    private async Task HandleOutgoing(CancellationToken token)
    {
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var frame = await outgoing.DequeueAsync(token).ConfigureAwait(false);
                await frameSocket.SendAsync(frame).ConfigureAwait(false);
            }
        }
        catch { }

        while (await outgoing.TryDequeueAsync().ConfigureAwait(false) is { } frame)
            await frameSocket.SendAsync(frame).ConfigureAwait(false);
    }

    private async Task HandleIncoming(CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var frame = await frameSocket.ReceiveAsync(token).ConfigureAwait(false);
            if (IsUserFacingFrame(frame))
                await incoming.EnqueueAsync(frame).ConfigureAwait(false);
            else
                await HandleInternalFrame(frame).ConfigureAwait(false);
        }
    }

    private async Task ReadCloseFrame(CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var frame = await frameSocket.ReceiveAsync(token).ConfigureAwait(false);
            if (frame.OpCode == FrameOpCode.Close)
                break;
        }
    }

    private async Task HandleInternalFrame(Frame frame)
    {
        if (frame.OpCode == FrameOpCode.Close && State == SocketState.Closing)
            closeConfirmationReceived.TrySetResult(true);
        else if (frame.OpCode == FrameOpCode.Close && State != SocketState.Closing)
        {
            peerRequestedClose = true;
            closeCts.Cancel();
        }
        else if (frame.OpCode == FrameOpCode.Ping)
            await frameSocket.SendAsync(Frame.Pong(frame)).ConfigureAwait(false);
    }

    private static bool IsUserFacingFrame(Frame frame) => frame.OpCode switch
    {
        FrameOpCode.Text => true,
        FrameOpCode.Binary => true,
        FrameOpCode.Continuation => true,
        _ => false
    };
}