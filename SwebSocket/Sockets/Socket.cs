namespace SwebSocket;

public abstract class Socket<T>
{
    private readonly SafeBlockingQueue<T> queue = new SafeBlockingQueue<T>();

    public Socket() => queue.OnEnqueue += (s, e) => OnMessage?.Invoke(this, e);

    private object statusGuard = new();
    private SocketStatus status;
    public SocketStatus Status
    {
        get { lock (statusGuard) return status; }
        protected set { lock (statusGuard) status = value; }
    }

    public event EventHandler<T>? OnMessage;
    public event EventHandler? OnClosed;
    public event EventHandler? OnConnected;

    public abstract void Close();
    public abstract Task CloseAsync();

    public abstract void Send(T message);
    public abstract Task SendAsync(T message);

    public virtual T Receive(CancellationToken token = default) => queue.Dequeue(token);
    public virtual Task<T> ReceiveAsync(CancellationToken token = default) => queue.DequeueAsync(token);

    protected void EmitMessage(T message) => queue.Enqueue(message);
    protected void EmitClosed() => OnClosed?.Invoke(this, EventArgs.Empty);
    protected void EmitConnected() => OnConnected?.Invoke(this, EventArgs.Empty);

    public void WaitForStatus(SocketStatus status, CancellationToken token = default)
    {
        while (Status != status && !token.IsCancellationRequested) Thread.Sleep(20);
    }

    public async Task WaitForStatusAsync(SocketStatus status, CancellationToken token = default)
    {
        while (Status != status && !token.IsCancellationRequested) await Task.Delay(20);
    }

    protected bool TestThenSetStatus(SocketStatus expected, SocketStatus desired)
        => TestThenSetStatus(s => s == expected, desired);

    protected bool TestThenSetStatus(Func<SocketStatus, bool> predicate, SocketStatus desired)
    {
        lock (statusGuard)
        {
            if (predicate.Invoke(status))
            {
                status = desired;
                return true;
            }
            else
                return false;
        }
    }
    protected SocketStatus StartTestThenSetStatus()
    {
        Monitor.Enter(statusGuard);
        return status;
    }
    protected void EndTestThenSetStatus(SocketStatus desired)
    {
        status = desired;
        Monitor.Exit(statusGuard);
    }
}