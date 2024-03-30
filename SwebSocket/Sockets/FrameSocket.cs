using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket
{
    internal class FrameSocket : Socket<Frame>, IDisposable
    {
        private Stream stream;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Task poller;
        private SemaphoreSlim sendLock = new SemaphoreSlim(1);
        private readonly MaskingBehavior masking;

        public FrameSocket(Stream stream, MaskingBehavior masking)
        {
            this.stream = stream;
            this.masking = masking;
            Status = SocketStatus.Connected;
            poller = Status == SocketStatus.Connected
                ? Task.Run(() => StartPolling(cts.Token), cts.Token)
                : Task.CompletedTask;
        }

        public override void Close()
        {
            if (TestThenSetStatus(SocketStatus.Connected, SocketStatus.Closing))
            {
                cts.Cancel();
                poller.Wait();
                sendLock.Wait();
                stream.Close();
                sendLock.Release();
                Status = SocketStatus.Closed;
                EmitClosed();
            }
        }

        public override async Task CloseAsync()
        {
            if (TestThenSetStatus(SocketStatus.Connected, SocketStatus.Closing))
            {
                cts.Cancel();
                await poller;
                await sendLock.WaitAsync();
                stream.Close();
                sendLock.Release();
                Status = SocketStatus.Closed;
                EmitClosed();
            }
        }

        public override void Send(Frame frame) => SendMany(new List<Frame> { frame });
        public override async Task SendAsync(Frame frame) => await SendManyAsync(new List<Frame> { frame });

        public void SendMany(IEnumerable<Frame> frames)
        {
            if (Status != SocketStatus.Connected)
                throw new SocketNotConnectedException();

            sendLock.Wait();

            if (Status != SocketStatus.Connected)
            {
                sendLock.Release();
                throw new SocketNotConnectedException();
            }

            try
            {
                var frameStream = new FrameStream(stream);
                foreach (var frame in frames)
                {
                    if (masking == MaskingBehavior.MaskOutgoing) frame.Mask();
                    frameStream.WriteFrame(frame);
                }
            }
            catch { }
            finally { sendLock.Release(); }
        }

        public async Task SendManyAsync(IEnumerable<Frame> frames, CancellationToken token = default)
        {
            if (Status != SocketStatus.Connected)
                throw new InvalidOperationException("Socket is not connected");

            await sendLock.WaitAsync();

            if (Status != SocketStatus.Connected)
            {
                sendLock.Release();
                throw new SocketNotConnectedException();
            }

            try
            {
                var frameStream = new FrameStream(stream);
                foreach (var frame in frames)
                {
                    if (masking == MaskingBehavior.MaskOutgoing) frame.Mask();
                    await frameStream.WriteFrameAsync(frame, token);
                }
            }
            catch { }
            finally { sendLock.Release(); }
        }

        private async Task StartPolling(CancellationToken token = default)
        {
            try
            {
                var frameStream = new FrameStream(stream);

                while (!token.IsCancellationRequested)
                {
                    var frame = await frameStream.ReadFrameAsync(token);
                    if (masking == MaskingBehavior.UnmaskIncoming) frame.Unmask();
                    EmitMessage(frame);
                }
            }
            catch (OperationCanceledException) { }
            catch
            {
                _ = Task.Run(Close);
            }
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                    WaitForStatus(SocketStatus.Closed);
                    cts.Dispose();
                    sendLock.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}