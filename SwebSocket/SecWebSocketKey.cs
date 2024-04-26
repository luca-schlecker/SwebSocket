
using System;
using System.Security.Cryptography;
using System.Text;

namespace SwebSocket;

internal class SecWebSocketKey
{
    public byte[] Nonce { get; }

    public string HeaderValue { get; }
    public string AcceptHeaderValue { get; }

    public SecWebSocketKey(byte[] nonce)
    {
        if (nonce.Length != 16)
            throw new ArgumentException("Nonce must be 4 bytes long");

        Nonce = nonce;
        HeaderValue = GetHeaderValue(nonce);
        AcceptHeaderValue = GetAcceptHeaderValue(HeaderValue);
    }

    public static SecWebSocketKey? FromHeader(string? header)
    {
        var nonce = GetNonce(header);
        if (nonce == null) return null;
        else return new SecWebSocketKey(nonce);
    }

    public static SecWebSocketKey Random()
    {
        var nonce = new byte[16];
        RandomNumberGenerator.Fill(nonce);
        return new SecWebSocketKey(nonce);
    }

    private static string GetHeaderValue(byte[] nonce) => Convert.ToBase64String(nonce);
    private static string GetAcceptHeaderValue(string headerValue)
    {
        var acceptString = headerValue + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var sha = SHA1.Create();
        var acceptHash = sha.ComputeHash(Encoding.UTF8.GetBytes(acceptString));
        return Convert.ToBase64String(acceptHash);
    }

    private static byte[]? GetNonce(string? headerValue)
    {
        if (headerValue == null || headerValue.Length == 0)
            return null;

        Span<byte> nonce = stackalloc byte[16];
        Convert.TryFromBase64String(headerValue, nonce, out var bytesWritten);
        if (bytesWritten != 16)
            return null;
        else return nonce.ToArray();
    }
}