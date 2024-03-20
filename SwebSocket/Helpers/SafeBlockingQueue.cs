using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket
{
    internal class SafeBlockingQueue<T>
    {
        private Queue<T> inputQueue = new Queue<T>();
        private Queue<T> outputQueue;
        private List<TaskCompletionSource<T>> waiters = new List<TaskCompletionSource<T>>();
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public event EventHandler<T>? OnEnqueue;

        public SafeBlockingQueue() { outputQueue = new Queue<T>(); }
        public SafeBlockingQueue(int capacity) { outputQueue = new Queue<T>(capacity); }

        public void Enqueue(T item)
        {
            lock (inputQueue) inputQueue.Enqueue(item);
            Task.Run(ProcessItem);
        }

        public T Dequeue(CancellationToken token = default)
        {
            TaskCompletionSource<T> waiter;
            lock (outputQueue)
            {
                if (outputQueue.Count > 0)
                    return outputQueue.Dequeue();
                else
                {
                    waiter = new TaskCompletionSource<T>();
                    waiters.Add(waiter);
                }
            }

            using (token.Register(() => waiter.TrySetCanceled()))
            {
                return waiter.Task.Result;
            }
        }

        public Task EnqueueAsync(T item)
        {
            Enqueue(item);
            return Task.CompletedTask;
        }

        public async Task<T> DequeueAsync(CancellationToken token = default)
        {
            TaskCompletionSource<T> waiter;
            lock (outputQueue)
            {
                if (outputQueue.Count > 0)
                    return outputQueue.Dequeue();
                else
                {
                    waiter = new TaskCompletionSource<T>();
                    waiters.Add(waiter);
                }
            }

            using (token.Register(() => waiter.TrySetCanceled()))
            {
                return await waiter.Task;
            }
        }

        private async void ProcessItem()
        {
            await semaphore.WaitAsync();
            T item;
            lock (inputQueue) item = inputQueue.Dequeue();

            OnEnqueue?.Invoke(this, item);
            lock (outputQueue)
            {
                if (waiters.Count > 0)
                {
                    waiters.ForEach(w => w.SetResult(item));
                    waiters.Clear();
                }
                else
                    outputQueue.Enqueue(item);
            }
            semaphore.Release();
        }
    }
}