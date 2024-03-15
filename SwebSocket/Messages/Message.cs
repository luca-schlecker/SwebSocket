using System.Text;

namespace SwebSocket;

public abstract class Message
{
    public IEnumerable<Frame> GetFrames(IMessageSplitter splitter)
    {
        var data = GetData();
        var chunks = splitter.Split(data);
        return chunks.Select((d, i) => MakeFrame(d) with
        {
            IsFinal = i == chunks.Count() - 1
        });
    }
    protected abstract byte[] GetData();
    protected abstract Frame MakeFrame(byte[] data);

    public sealed class Text : Message
    {
        public string Data { get; }
        public Text(string data) => Data = data;

        protected override byte[] GetData()
            => Encoding.UTF8.GetBytes(Data);

        protected override Frame MakeFrame(byte[] data)
            => new Frame(FrameOpCode.Text, data);
    }

    public sealed class Binary : Message
    {
        public byte[] Data { get; }
        public Binary(byte[] data) => Data = data;

        protected override byte[] GetData() => Data;

        protected override Frame MakeFrame(byte[] data)
            => new Frame(FrameOpCode.Binary, data);
    }
}