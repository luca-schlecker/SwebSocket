
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket;

public class Frame
{
    public bool IsFinal { get; set; }
    public FrameOpCode OpCode { get; set; }
    public byte[]? MaskingKey { get; set; }
    public byte[] Payload { get; set; }

    public bool IsMasked => MaskingKey != null;

    public byte[] ToArray()
    {
        byte[] header;
        if (Payload.Length < 126)
            header = new byte[] { 0, (byte)Payload.Length };
        else if (Payload.Length <= ushort.MaxValue)
            header = new byte[] { 0, 126 }.Concat(BitConverter.GetBytes((ushort)Payload.Length)).ToArray();
        else
            header = new byte[] { 0, 127 }.Concat(BitConverter.GetBytes((ulong)Payload.LongLength)).ToArray();

        header[0] |= IsFinal ? (byte)0x80 : (byte)0x00;
        header[0] |= (byte)OpCode;
        header[1] |= IsMasked ? (byte)0x80 : (byte)0x00;

        var mask = IsMasked ? MaskingKey : Array.Empty<byte>();

        return header.Concat(mask).Concat(Payload).ToArray();
    }
    public static async Task<Frame> FromStream(Stream stream, CancellationToken token)
    {
        var header = new byte[2];
        var read = await stream.ReadAsync(header, token);
        if (read == 0) throw new EndOfStreamException();
        var isMasked = (header[1] & 0x80) == 0x80;
        var isFinal = (header[0] & 0x80) == 0x80;
        var opCode = (FrameOpCode)(header[0] & 0x0F);
        var payloadLength = (header[1] & 0x7F) switch
        {
            126 => new byte[2],
            127 => new byte[8],
            _ => new byte[0],
        };
        if (payloadLength.Length != 0)
        {
            read = await stream.ReadAsync(payloadLength, token);
            if (read == 0) throw new EndOfStreamException();
        }
        var realLength = payloadLength.Length switch
        {
            2 => BitConverter.ToUInt16(payloadLength),
            8 => BitConverter.ToUInt64(payloadLength),
            _ => (ulong)(header[1] & 0x7F),
        };
        var maskingKey = isMasked ? new byte[4] : null;
        if (maskingKey != null)
        {
            read = await stream.ReadAsync(maskingKey, token);
            if (read == 0) throw new EndOfStreamException();
        }
        var payload = new byte[realLength];
        if (realLength != 0)
        {
            read = await stream.ReadAsync(payload, token);
            if (read == 0) throw new EndOfStreamException();
        }
        return new Frame(isFinal, opCode, maskingKey, payload);
    }

    #region Masking
    public Frame Mask()
    {
        if (IsMasked) return this;

        Span<byte> mask = stackalloc byte[4];
        new Random().NextBytes(mask);
        MaskingKey = mask.ToArray();
        for (int i = 0; i < Payload.Length; i++)
            Payload[i] = (byte)(Payload[i] ^ mask[i % 4]);
        return this;
    }
    public Frame Unmask()
    {
        if (!IsMasked) return this;

        for (int i = 0; i < Payload.Length; i++)
            Payload[i] = (byte)(Payload[i] ^ MaskingKey![i % 4]);
        MaskingKey = null;
        return this;
    }
    #endregion
    #region Construction
    public Frame(Span<byte> data)
    {
        var maskSize = (data[1] & 0x80) == 0x80 ? 4 : 0;
        var additionalPayloadSize = (data[1] & 0x7F) switch
        {
            126 => 2,
            127 => 8,
            _ => 0
        };

        IsFinal = (data[0] & 0x80) == 0x80;
        OpCode = (FrameOpCode)(data[0] & 0x0F);
        MaskingKey = (data[1] & 0x80) == 0x80 ? data.Slice(2 + additionalPayloadSize, 4).ToArray() : null;
        Payload = data.Slice(2 + maskSize + additionalPayloadSize).ToArray();
    }
    public Frame(FrameOpCode opCode, byte[] payload) : this(true, opCode, null, payload) { }
    public Frame(bool isFinal, FrameOpCode opCode, byte[]? maskingKey, byte[] payload)
    {
        IsFinal = isFinal;
        OpCode = opCode;
        MaskingKey = maskingKey;
        Payload = payload;
    }

    public static Frame Ping() => new Frame(FrameOpCode.Ping, Array.Empty<byte>());
    public static Frame Ping(byte[] payload) => new Frame(FrameOpCode.Ping, payload);
    public static Frame Pong(Frame frame)
    {
        if (frame.OpCode != FrameOpCode.Ping)
            throw new ArgumentException("Pong frame must be a response to a ping frame");

        return new Frame(FrameOpCode.Pong, frame.Payload);
    }
    public static Frame Close(FrameCloseStatusCode statusCode = FrameCloseStatusCode.NormalClosure, string? reason = null)
    {
        var payloadData = Encoding.UTF8.GetBytes(reason ?? "");
        var statusData = BitConverter.GetBytes((ushort)statusCode);

        return new Frame(FrameOpCode.Close, statusData.Concat(payloadData).ToArray());
    }
    public static Frame Text(string message) => new Frame(FrameOpCode.Text, Encoding.UTF8.GetBytes(message));
    public static Frame Binary(byte[] data) => new Frame(FrameOpCode.Binary, data);
    #endregion
}

public enum FrameOpCode : byte
{
    Continuation = 0x0,
    Text = 0x1,
    Binary = 0x2,
    Close = 0x8,
    Ping = 0x9,
    Pong = 0xA
}

public enum FrameCloseStatusCode : ushort
{
    NormalClosure = 1000,
    GoingAway = 1001,
    ProtocolError = 1002,
    UnsupportedData = 1003,
    InvalidFramePayloadData = 1007,
    PolicyViolation = 1008,
    MessageTooBig = 1009,
    MandatoryExt = 1010,
    InternalServerError = 1011,
}