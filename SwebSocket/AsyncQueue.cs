
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket
{
    /// <summary>
    /// A queue that can be used to safely enqueue and dequeue items across multiple threads.
    /// It provides asynchronous, non-blocking and blocking methods.
    /// </summary>
    public class AsyncQueue<T> where T : class
    {
        /// <summary>
        /// Wether the queue is closed or not.
        /// </summary>
        public bool IsClosed => cts.IsCancellationRequested;

        /// <summary>
        /// The number of items available in the queue.
        /// </summary>
        public int Count => queue.Count;

        private SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private Queue<T> queue = new Queue<T>();
        private Queue<TaskCompletionSource<T>> waiters = new Queue<TaskCompletionSource<T>>();
        private CancellationTokenSource cts = new CancellationTokenSource();

        public AsyncQueue() { }

        /// <summary>
        /// Closes the queue and cancels all pending operations.
        /// This method will return immediately.
        /// </summary>
        /// <remarks>
        /// This method is idempotent. Calling it multiple times will have no effect.
        /// </remarks>
        public void Close() { }

        /// <summary>
        /// Enqueue an item.
        /// This method will return immediately.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is closed.</exception>
        public void Enqueue(T item) { }

        /// <summary>
        /// Enqueue an item.
        /// This Task will complete immediately.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is closed.</exception>
        public async Task EnqueueAsync(T item) => await Task.CompletedTask;

        /// <summary>
        /// Dequeue an item.
        /// This method will block until an item is available.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is closed.</exception>
        /// <exception cref="OperationCanceledException">The queue was closed while waiting for an item.</exception>
        public T Dequeue() => default;

        /// <summary>
        /// Dequeue an item.
        /// This Task will complete successfully once an item becomes available.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is closed.</exception>
        /// <exception cref="OperationCanceledException">The queue was closed while waiting for an item.</exception>
        public async Task<T> DequeueAsync(CancellationToken token) => null;

        /// <summary>
        /// Try to dequeue an item.
        /// This method will return immediately.
        /// </summary>
        /// <returns>The next item from the queue, or <c>null</c> if there were no items.</returns>
        /// <exception cref="InvalidOperationException">The queue is closed.</exception>
        public T? TryDequeue() => null;

        /// <summary>
        /// Try to dequeue an item.
        /// This Task will complete immediately.
        /// </summary>
        /// <returns>The next item from the queue, or <c>null</c> if there were no items.</returns>
        /// <exception cref="InvalidOperationException">The queue is closed.</exception>
        public async Task<T?> TryDequeueAsync(CancellationToken token = default) => null;
    }
}