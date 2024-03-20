using System.Collections.Generic;

namespace SwebSocket
{
    public interface IMessageSplitter
    {
        public IEnumerable<byte[]> Split(byte[] data);
    }

    public class DefaultMessageSplitter : IMessageSplitter
    {
        public IEnumerable<byte[]> Split(byte[] data) => new byte[][] { data };
    }
}