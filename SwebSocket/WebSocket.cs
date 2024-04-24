
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket
{
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
        public SocketState State { get; private set; }

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
        public void Send(Message message) { }

        /// <summary>
        /// Queues a message to be sent to the peer.
        /// This Task will complete immediately.
        /// If the WebSocket is currently connecting, the message will be queued and sent once the WebSocket is connected.
        /// If the WebSocket is currently closing, the message will be discarded silently.
        /// </summary>
        /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
        public async Task SendAsync(Message message) { }

        /// <summary>
        /// Return the next message from the peer.
        /// This method will block until a message is received.
        /// </summary>
        /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
        /// <exception cref="OperationCanceledException">The WebSocket is being closed and thus this function could never return.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled using the given token.</exception>
        public Message Receive(CancellationToken token = default) { return new TextMessage(""); }

        /// <summary>
        /// Return the next message from the peer.
        /// This Task will complete successfully once a message has been received.
        /// </summary>
        /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
        /// <exception cref="OperationCanceledException">The WebSocket is being closed and thus this function could never return.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled using the given token.</exception>
        public async Task<Message> ReceiveAsync(CancellationToken token = default) => new TextMessage("");

        /// <summary>
        /// Try to receive a message from the peer.
        /// This method will return immediately.
        /// </summary>
        /// <returns>The next message from the peer, or <c>null</c> if there were no messages or the WebSocket is being closed.</returns>
        /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
        public Message? TryReceive() => new TextMessage("");

        /// <summary>
        /// Try to receive a message from the peer.
        /// This Task will complete immediately.
        /// </summary>
        /// <returns>The next message from the peer, or <c>null</c> if there were no messages or the WebSocket is being closed.</returns>
        /// <exception cref="InvalidOperationException">The WebSocket is closed.</exception>
        public async Task<Message?> TryReceiveAsync() => new TextMessage("");

        /// <summary>
        /// Close the WebSocket.
        /// This method will return immediately, leaving the WebSocket in a <c>Closing</c> state.
        /// </summary>
        /// <remarks>
        /// This method is idempotent. Calling it multiple times will have no effect.
        /// </remarks>
        public void Close() { }
    }
}