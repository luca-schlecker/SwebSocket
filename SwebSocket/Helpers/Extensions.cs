using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket
{
    public static class Extensions
    {
        internal static async Task<byte[]> ReadUntilAsync(this Stream s, string delimiter, CancellationToken token = default)
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

        internal static async Task ReadExactlyAsync(this Stream s, byte[] buffer, int offset, int count, CancellationToken token = default)
        {
            while (count > 0)
            {
                token.ThrowIfCancellationRequested();
                var bytesRead = await s.ReadAsync(buffer, offset, count, token);
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        internal static Task ReadExactlyAsync(this Stream s, byte[] buffer, CancellationToken token = default)
            => ReadExactlyAsync(s, buffer, 0, buffer.Length, token);

        internal static void ReadExactly(this Stream s, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                var bytesRead = s.Read(buffer, offset, count);
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        internal static void ReadExactly(this Stream s, Span<byte> buffer)
        {
            var bytesRead = 0;

            while (bytesRead != buffer.Length)
            {
                var bytesReadThisTime = s.Read(buffer.Slice(bytesRead));
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                bytesRead += bytesReadThisTime;
            }
        }

        internal static string? GetHttpHeader(this IDictionary<string, IEnumerable<string>> headers, string key)
        {
            key = key.ToUpper(); // HTTPMachine uses upper case keys
            if (!headers.ContainsKey(key))
                return null;
            return string.Join(", ", headers[key]);
        }
    }
}