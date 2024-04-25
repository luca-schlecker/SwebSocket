
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket;

internal class FrameSocket
{
    private bool isClient;
    private Socket socket;
    private Stream stream;

    public FrameSocket(Socket socket, Stream stream, bool isClient)
    {
        this.socket = socket;
        this.stream = stream;
        this.isClient = isClient;
    }

    public void Close() => socket.Close();

    public async Task SendAsync(Frame frame)
    {
        if (isClient) frame.Mask();
        await stream.WriteAsync(frame.ToArray());
    }

    public async Task<Frame> ReceiveAsync(CancellationToken token)
    {
        var frame = await Frame.FromStream(stream, token);
        return !isClient ? frame.Unmask() : frame;
    }
}