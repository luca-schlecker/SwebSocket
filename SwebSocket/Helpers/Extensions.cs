using System.Net.Sockets;
using System.Text;

namespace SwebSocket;

public static class Extensions
{
    internal static async Task<byte[]> ReadUntilAsync(this NetworkStream s, string delimiter, CancellationToken token = default)
    {
        var delimiterBytes = Encoding.ASCII.GetBytes(delimiter);
        var buffer = new byte[1024];
        var bytesRead = 0;

        await s.ReadExactlyAsync(buffer, bytesRead, delimiterBytes.Length, token);
        bytesRead += delimiterBytes.Length;

        while (buffer.AsSpan(bytesRead - delimiterBytes.Length, delimiterBytes.Length).IndexOf(delimiterBytes) == -1)
        {
            await s.ReadExactlyAsync(buffer, bytesRead, 1, token);
            bytesRead += 1;

            if (bytesRead >= buffer.Length)
                throw new Exception("Delimiter not found, buffer full");
        }

        return buffer.AsSpan(0, bytesRead).ToArray();
    }

    internal static string? GetHttpHeader(this IDictionary<string, IEnumerable<string>> headers, string key)
    {
        key = key.ToUpper(); // HTTPMachine uses upper case keys
        if (!headers.ContainsKey(key))
            return null;
        return string.Join(", ", headers[key]);
    }
}