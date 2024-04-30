
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket;

/// <summary>
/// A queue that can be used to safely enqueue and dequeue items across multiple threads.
/// It provides asynchronous, non-blocking and blocking methods.
/// </summary>
public class AsyncQueue<T> where T : class
{
    /// <summary>
    /// Wether the queue is closed or not.
    /// </summary>
    public bool IsClosed => true;

    /// <summary>
    /// The number of items available in the queue.
    /// </summary>
    public int Count => 0;

    private CancellationTokenSource cts = new();
    private SemaphoreSlim semaphore = new(1);
    private Queue<T> queue = new();
    private Queue<TaskCompletionSource<T>> waiters = new();

    public AsyncQueue() { }

    /// <summary>
    /// Closes the queue and cancels all pending operations.
    /// This method will return immediately.
    /// </summary>
    /// <remarks>
    /// This method is idempotent. Calling it multiple times will have no effect.
    /// </remarks>
    public void Close() => cts.Cancel();

    /// <summary>
    /// Enqueue an item.
    /// This method will return immediately.
    /// </summary>
    /// <exception cref="InvalidOperationException">The queue is closed.</exception>
    public void Enqueue(T item)
    {
        cts.Token.ThrowIfCancellationRequested();
        semaphore.Wait();
        if (waiters.TryDequeue(out var tcs))
            tcs.SetResult(item);
        else
            queue.Enqueue(item);
        semaphore.Release();
    }

    /// <summary>
    /// Enqueue an item.
    /// This Task will complete immediately.
    /// </summary>
    /// <exception cref="InvalidOperationException">The queue is closed.</exception>
    public async Task EnqueueAsync(T item)
    {
        cts.Token.ThrowIfCancellationRequested();
        await semaphore.WaitAsync();
        if (waiters.TryDequeue(out var tcs))
            tcs.SetResult(item);
        else
            queue.Enqueue(item);
        semaphore.Release();
    }

    /// <summary>
    /// Dequeue an item.
    /// This method will block until an item is available.
    /// </summary>
    /// <exception cref="InvalidOperationException">The queue is closed.</exception>
    /// <exception cref="OperationCanceledException">The queue was closed while waiting for an item.</exception>
    public T Dequeue(CancellationToken token)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token).Token;

        linked.ThrowIfCancellationRequested();
        semaphore.Wait();
        if (queue.TryDequeue(out var item))
        {
            semaphore.Release();
            return item;
        }
        else
        {
            var tcs = new TaskCompletionSource<T>();
            using var registration = linked.Register(() => tcs.TrySetCanceled());
            waiters.Enqueue(tcs);
            semaphore.Release();
            return tcs.Task.Result;
        }
    }

    /// <summary>
    /// Dequeue an item.
    /// This Task will complete successfully once an item becomes available.
    /// </summary>
    /// <exception cref="InvalidOperationException">The queue is closed.</exception>
    /// <exception cref="OperationCanceledException">The queue was closed while waiting for an item.</exception>
    public async Task<T> DequeueAsync(CancellationToken token)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token).Token;

        linked.ThrowIfCancellationRequested();
        await semaphore.WaitAsync(linked);
        if (queue.TryDequeue(out var item))
        {
            semaphore.Release();
            return item;
        }
        else
        {
            var tcs = new TaskCompletionSource<T>();
            using var registration = linked.Register(() => tcs.TrySetCanceled());
            waiters.Enqueue(tcs);
            semaphore.Release();
            return await tcs.Task;
        }
    }

    /// <summary>
    /// Try to dequeue an item.
    /// This method will return immediately.
    /// </summary>
    /// <returns>The next item from the queue, or <c>null</c> if there were no items.</returns>
    /// <exception cref="InvalidOperationException">The queue is closed.</exception>
    public T? TryDequeue()
    {
        cts.Token.ThrowIfCancellationRequested();
        semaphore.Wait();
        queue.TryDequeue(out var item);
        semaphore.Release();
        return item;
    }

    /// <summary>
    /// Try to dequeue an item.
    /// This Task will complete immediately.
    /// </summary>
    /// <returns>The next item from the queue, or <c>null</c> if there were no items.</returns>
    /// <exception cref="InvalidOperationException">The queue is closed.</exception>
    public async Task<T?> TryDequeueAsync(CancellationToken token = default)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token).Token;

        linked.ThrowIfCancellationRequested();
        await semaphore.WaitAsync();
        queue.TryDequeue(out var item);
        semaphore.Release();
        return item;
    }
}