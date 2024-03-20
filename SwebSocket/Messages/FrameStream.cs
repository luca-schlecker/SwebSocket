using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SwebSocket
{
    internal class FrameStream
    {
        private Stream _stream;

        public FrameStream(Stream stream)
        {
            _stream = stream;
        }

        public Frame ReadFrame()
        {
            Span<byte> buffer = stackalloc byte[2];
            _stream.ReadExactly(buffer);

            var isFinal = (buffer[0] & 0b1000_0000) != 0;
            var opCode = (FrameOpCode)(buffer[0] & 0b0000_1111);
            var payloadLen = buffer[1] & 0b0111_1111;
            var isMasked = (buffer[1] & 0b1000_0000) != 0;
            ulong realLength = ReadRealLength(payloadLen);
            var maskingKey = new byte[4];
            if (isMasked) _stream.ReadExactly(maskingKey);
            var payload = new byte[realLength];
            _stream.ReadExactly(payload);

            return new Frame()
            {
                IsFinal = isFinal,
                OpCode = opCode,
                MaskingKey = isMasked ? maskingKey : null,
                Payload = payload
            };
        }

        public async Task<Frame> ReadFrameAsync(CancellationToken token = default)
        {
            var buffer = new byte[2];
            await _stream.ReadExactlyAsync(buffer, token);

            var isFinal = (buffer[0] & 0b1000_0000) != 0;
            var opCode = (FrameOpCode)(buffer[0] & 0b0000_1111);
            var payloadLen = buffer[1] & 0b0111_1111;
            var isMasked = (buffer[1] & 0b1000_0000) != 0;
            ulong realLength = await ReadRealLengthAsync(payloadLen, token);
            var maskingKey = new byte[4];
            if (isMasked) await _stream.ReadExactlyAsync(maskingKey, token);
            var payload = new byte[realLength];
            await _stream.ReadExactlyAsync(payload, token);

            return new Frame()
            {
                IsFinal = isFinal,
                OpCode = opCode,
                MaskingKey = isMasked ? maskingKey : null,
                Payload = payload
            };
        }

        private ulong ReadRealLength(int payloadLength)
        {
            if (payloadLength < 126) return (ulong)payloadLength;
            else if (payloadLength == 126)
            {
                var buffer = new byte[2];
                _stream.ReadExactly(buffer);
                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
                return BitConverter.ToUInt16(buffer);
            }
            else
            {
                var buffer = new byte[8];
                _stream.ReadExactly(buffer);
                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
                return BitConverter.ToUInt64(buffer);
            }
        }

        private async Task<ulong> ReadRealLengthAsync(int payloadLength, CancellationToken token = default)
        {
            if (payloadLength < 126) return (ulong)payloadLength;
            else if (payloadLength == 126)
            {
                var buffer = new byte[2];
                await _stream.ReadExactlyAsync(buffer, token);
                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
                return BitConverter.ToUInt16(buffer);
            }
            else
            {
                var buffer = new byte[8];
                await _stream.ReadExactlyAsync(buffer, token);
                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
                return BitConverter.ToUInt64(buffer);
            }
        }

        public void WriteFrame(Frame frame)
        {
            var payloadLengthValue = GetPayloadLengthValue(frame.Payload.LongLength);
            var header = GetPayloadHeader(frame, payloadLengthValue);
            _stream.Write(header);
            WriteExtendedPayloadLength((ulong)frame.Payload.LongLength);
            if (frame.IsMasked) _stream.Write(frame.MaskingKey);
            _stream.Write(frame.Payload);
        }

        public async Task WriteFrameAsync(Frame frame, CancellationToken token = default)
        {
            var payloadLengthValue = GetPayloadLengthValue(frame.Payload.LongLength);
            var header = GetPayloadHeader(frame, payloadLengthValue);
            await _stream.WriteAsync(header, token);
            await WriteExtendedPayloadLengthAsync((ulong)frame.Payload.LongLength, token);
            if (frame.IsMasked) await _stream.WriteAsync(frame.MaskingKey, token);
            await _stream.WriteAsync(frame.Payload, token);
        }

        private void WriteExtendedPayloadLength(ulong length)
        {
            if (length < 126) { }
            else if (length <= ushort.MaxValue)
            {
                var buffer = BitConverter.GetBytes((ushort)length);
                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
                _stream.Write(buffer);
            }
            else
            {
                var buffer = BitConverter.GetBytes(length);
                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
                _stream.Write(buffer);
            }
        }

        private async Task WriteExtendedPayloadLengthAsync(ulong length, CancellationToken token = default)
        {
            if (length < 126) { }
            else if (length <= ushort.MaxValue)
            {
                var buffer = BitConverter.GetBytes((ushort)length);
                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
                await _stream.WriteAsync(buffer, token);
            }
            else
            {
                var buffer = BitConverter.GetBytes(length);
                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
                await _stream.WriteAsync(buffer, token);
            }
        }

        private static byte[] GetPayloadHeader(Frame frame, int payloadLengthValue)
        {
            var higher = (frame.IsFinal ? 0b1000_0000 : 0) | ((int)frame.OpCode & 0b0000_1111);
            var lower = (frame.IsMasked ? 0b1000_0000 : 0) | (payloadLengthValue & 0b0111_1111);
            return new byte[] { (byte)higher, (byte)lower };
        }

        private static int GetPayloadLengthValue(long payloadLength)
        {
            if (payloadLength < 126)
                return (int)payloadLength;
            else if (payloadLength <= ushort.MaxValue)
                return 126;
            else
                return 127;
        }
    }
}