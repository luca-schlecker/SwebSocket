namespace SwebSocat;

class ResettableCancellationTokenSource : IDisposable
{
    private CancellationTokenSource cts = new();

    public CancellationToken Token => cts.Token;
    public void CancelAndReset()
    {
        cts.Cancel();
        cts.Dispose();
        cts = new();
    }

    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                cts.Dispose();

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}