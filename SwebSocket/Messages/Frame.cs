using System;
using System.Text;

namespace SwebSocket
{
    public struct Frame
    {
        public bool IsFinal;
        public FrameOpCode OpCode;
        public byte[]? MaskingKey;
        public byte[] Payload;

        public bool IsMasked => MaskingKey != null;

        #region Masking
        public Frame Mask()
        {
            if (IsMasked)
                throw new InvalidOperationException("Frame is already masked");

            Span<byte> mask = stackalloc byte[4];
            new Random().NextBytes(mask);
            MaskingKey = mask.ToArray();
            for (int i = 0; i < Payload.Length; i++)
                Payload[i] = (byte)(Payload[i] ^ mask[i % 4]);
            return this;
        }

        public Frame Unmask()
        {
            if (!IsMasked)
                throw new InvalidOperationException("Frame is not masked");

            for (int i = 0; i < Payload.Length; i++)
                Payload[i] = (byte)(Payload[i] ^ MaskingKey![i % 4]);
            MaskingKey = null;
            return this;
        }
        #endregion

        #region Construction
        public Frame(FrameOpCode opCode, byte[] payload) : this(true, opCode, null, payload) { }

        public Frame(bool isFinal, FrameOpCode opCode, byte[]? maskingKey, byte[] payload)
        {
            IsFinal = isFinal;
            OpCode = opCode;
            MaskingKey = maskingKey;
            Payload = payload;
        }

        public static Frame Ping() => Ping(Array.Empty<byte>());
        public static Frame Ping(byte[] payload) => new Frame(FrameOpCode.Ping, payload);
        public static Frame Pong(Frame frame)
        {
            if (frame.OpCode != FrameOpCode.Ping)
                throw new ArgumentException("Pong frame must be a response to a ping frame");

            return new Frame(FrameOpCode.Pong, frame.Payload);
        }
        public static Frame Close(CloseStatusCode? statusCode = null, string? reason = null)
        {
            Span<byte> payload = stackalloc byte[(statusCode != null ? 2 : 0) + Encoding.UTF8.GetByteCount(reason ?? "")];
            if (statusCode != null)
                BitConverter.TryWriteBytes(payload, (ushort)statusCode);
            Encoding.UTF8.GetBytes(reason ?? "", payload.Slice(statusCode != null ? 2 : 0));
            return new Frame(FrameOpCode.Close, payload.ToArray());
        }
        public static Frame Text(string message) => new Frame(FrameOpCode.Text, Encoding.UTF8.GetBytes(message));
        public static Frame Binary(byte[] data) => new Frame(FrameOpCode.Binary, data);
        #endregion
    }
}