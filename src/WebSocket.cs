
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket;

public enum SocketState
{
    /// <summary>
    /// The WebSocket is initialized but has not yet completed the WebSocket handshake.
    /// </summary>
    Connecting,

    /// <summary>
    /// The WebSocket is connected and can send and receive messages.
    /// </summary>
    Connected,

    /// <summary>
    /// The WebSocket is closing and will not forward any messages.
    /// The closing handshake has not yet completed.
    /// </summary>
    Closing,

    /// <summary>
    /// The WebSocket is closed. Sending messages will result in exceptions.
    /// </summary>
    Closed,
}

public class WebSocket
{
    /// <summary>
    /// The current state of the WebSocket.
    /// </summary>
    public SocketState State => socket.State;

    /// <summary>
    /// The number of messages that are currently in the incoming queue.
    /// These messages can be retreived without delay using the <see cref="Receive"/> and <see cref="ReceiveAsync"/> methods.
    /// </summary>
    public int Available => incoming.Count;

    /// <remarks>
    /// This event doesn't get raised on its own!
    /// Use a MessagePoller if you want to use this event.
    /// </remarks>
    /// <seealso cref="EmitMessage"/>
    public event EventHandler<Message>? OnMessage;

    /// <summary>
    /// Invoke the <see cref="OnMessage"/> event.
    /// </summary>
    public void EmitMessage(Message message) => OnMessage?.Invoke(this, message);

    /// <summary>
    /// This event is raised when the WebSocket has successfully established a connection.
    /// </summary>
    public event EventHandler? OnConnected
    {
        add => socket.OnConnected += value;
        remove => socket.OnConnected -= value;
    }

    /// <summary>
    /// This event is raised when the WebSocket is preparing to close.
    /// </summary>
    public event EventHandler? OnClosing;

    /// <summary>
    /// This event is raised when the WebSocket has fully closed.
    /// </summary>
    public event EventHandler? OnClosed;

    /// <summary>
    /// Connect to a WebSocket using the WebSocketBuilder interface.
    /// </summary>
    public static WebSocketBuilder Connect() => new WebSocketBuilder();

    /// <summary>
    /// Queues a message to be sent to the peer.
    /// This method will return immediately.
    /// If the WebSocket is currently connecting, the message will be queued and sent once the WebSocket is connected.
    /// If the WebSocket is currently closing, the message will be discarded silently.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
    public void Send(Message message)
    {
        ThrowIfClosed();
        socket.SendRange(FramesFromMessage(message));
    }

    /// <summary>
    /// This convenience-method calls <see cref="Send(Message)"/> with a new <see cref="TextMessage"/> containing the provided string.
    /// </summary>
    /// <remarks>
    /// See <see cref="Send(Message)"/> for more information.
    /// </remarks>
    public void Send(string text) => Send(new TextMessage(text));

    /// <summary>
    /// This convenience-method calls <see cref="Send(Message)"/> with a new <see cref="BinaryMessage"/> containing the provided data.
    /// </summary>
    /// <remarks>
    /// See <see cref="Send(Message)"/> for more information.
    /// </remarks>
    public void Send(byte[] data) => Send(new BinaryMessage(data));

    /// <summary>
    /// Queues a message to be sent to the peer.
    /// This Task will complete immediately.
    /// If the WebSocket is currently connecting, the message will be queued and sent once the WebSocket is connected.
    /// If the WebSocket is currently closing, the message will be discarded silently.
    /// </summary>
    /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
    public async Task SendAsync(Message message)
    {
        ThrowIfClosed();
        await socket.SendRangeAsync(FramesFromMessage(message));
    }

    /// <summary>
    /// This convenience-method calls <see cref="SendAsync(Message)"/> with a new <see cref="TextMessage"/> containing the provided string.
    /// </summary>
    /// <remarks>
    /// See <see cref="Send(Message)"/> for more information.
    /// </remarks>
    public Task SendAsync(string text) => SendAsync(new TextMessage(text));

    /// <summary>
    /// This convenience-method calls <see cref="SendAsync(Message)"/> with a new <see cref="BinaryMessage"/> containing the provided data.
    /// </summary>
    /// <remarks>
    /// See <see cref="Send(Message)"/> for more information.
    /// </remarks>
    public Task SendAsync(byte[] data) => SendAsync(new BinaryMessage(data));

    /// <summary>
    /// Return the next message from the peer.
    /// This method will block until a message is received.
    /// </summary>
    /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
    /// <exception cref="OperationCanceledException">The WebSocket is being closed and thus this function could never return.</exception>
    /// <exception cref="OperationCanceledException">The operation has been canceled using the given token.</exception>
    public Message Receive(CancellationToken token = default)
    {
        ThrowIfClosed();
        return incoming.Dequeue(token);
    }

    /// <summary>
    /// Return the next message from the peer.
    /// This Task will complete successfully once a message has been received.
    /// </summary>
    /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
    /// <exception cref="OperationCanceledException">The WebSocket is being closed and thus this function could never return.</exception>
    /// <exception cref="OperationCanceledException">The operation has been canceled using the given token.</exception>
    public async Task<Message> ReceiveAsync(CancellationToken token = default)
    {
        ThrowIfClosed();
        return await incoming.DequeueAsync(token);
    }

    /// <summary>
    /// Try to receive a message from the peer.
    /// This method will return immediately.
    /// </summary>
    /// <returns>The next message from the peer, or <c>null</c> if there were no messages or the WebSocket is being closed.</returns>
    /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
    public Message? TryReceive()
    {
        ThrowIfClosed();
        return incoming.TryDequeue();
    }

    /// <summary>
    /// Try to receive a message from the peer.
    /// This Task will complete immediately.
    /// </summary>
    /// <returns>The next message from the peer, or <c>null</c> if there were no messages or the WebSocket is being closed.</returns>
    /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
    public async Task<Message?> TryReceiveAsync()
    {
        ThrowIfClosed();
        return await incoming.TryDequeueAsync();
    }

    /// <summary>
    /// Close the WebSocket.
    /// This method will return immediately, leaving the WebSocket in a <c>Closing</c> state.
    /// </summary>
    /// <remarks>
    /// This method is idempotent. Calling it multiple times will have no effect.
    /// </remarks>
    public void Close() => socket.Close();

    private ConnectionFrameSocket socket;
    private AsyncQueue<Message> incoming = new();

    internal WebSocket(ConnectionFrameSocket socket)
    {
        this.socket = socket;
        socket.OnClosed += (_, e) =>
        {
            incoming.Close();
            OnClosed?.Invoke(this, e);
        };
        socket.OnClosing += (_, e) => OnClosing?.Invoke(this, e);
        Task.Run(StartLifecycle);
    }

    private async Task StartLifecycle()
    {
        try { await HandleIncoming(); }
        catch { }
    }

    private async Task HandleIncoming()
    {
        var queue = new Queue<Frame>();
        while (true)
        {
            var frame = await socket.ReceiveAsync(default);
            queue.Enqueue(frame);
            if (frame.IsFinal)
            {
                var message = MessageFromFrames(queue);
                await incoming.EnqueueAsync(message);
                queue.Clear();
            }
        }
    }

    private Frame[] FramesFromMessage(Message message)
    {
        return message switch
        {
            TextMessage text => new Frame[] { Frame.Text(text.Text) },
            BinaryMessage binary => new Frame[] { Frame.Binary(binary.Data) },
            _ => throw new InvalidOperationException("Unknown message type")
        };
    }

    private Message MessageFromFrames(Queue<Frame> queue)
    {
        var first = queue.Peek();
        var data = queue.SelectMany(frame => frame.Payload).ToArray();

        Message message = first.OpCode switch
        {
            FrameOpCode.Text => new TextMessage(System.Text.Encoding.UTF8.GetString(data)),
            FrameOpCode.Binary => new BinaryMessage(data),
            _ => throw new InvalidOperationException("Unknown OpCode")
        };

        return message;
    }

    private void ThrowIfClosed()
    {
        if (State == SocketState.Closed)
            throw new InvalidOperationException("The WebSocket is closed.");
    }
}