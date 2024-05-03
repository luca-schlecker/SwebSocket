
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket;

internal static class StreamHelper
{
    public static async Task ReadExactly(this Stream stream, byte[] buffer, CancellationToken token)
    {
        if (buffer.Length == 0) return;

        int alreadyRead = 0;
        int toBeRead = buffer.Length;

        while (alreadyRead != toBeRead)
        {
            var actuallyRead = await stream.ReadAsync(buffer, alreadyRead, toBeRead - alreadyRead, token);
            if (actuallyRead == 0) throw new EndOfStreamException();
            alreadyRead += actuallyRead;
        }
    }

    public static async Task<byte[]> ReadExactly(this Stream stream, int bytes, CancellationToken token)
    {
        var buffer = new byte[bytes];
        await ReadExactly(stream, buffer, token);

        return buffer;
    }
}