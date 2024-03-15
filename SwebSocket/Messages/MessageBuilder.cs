
using System.Text;

namespace SwebSocket;

internal class MessageBuilder
{
    private List<Frame> frames = new();

    public MessageBuilder() { }

    public Message? Append(Frame frame)
    {
        if (frame.OpCode != FrameOpCode.Continuation
         && frame.OpCode != FrameOpCode.Text
         && frame.OpCode != FrameOpCode.Binary)
            throw new UnexpectedFrameException();

        lock (frames)
        {
            if (frame.OpCode == FrameOpCode.Continuation)
            {
                if (frames.Count == 0) throw new UnexpectedFrameException();
                else frames.Add(frame);
            }
            else
            {
                if (frames.Count != 0) throw new MessagesInterleavedException();
                else frames.Add(frame);
            }

            if (frames.Last().IsFinal)
            {
                var message = MakeMessage();
                frames.Clear();
                return message;
            }
        }
        return null;
    }

    private Message MakeMessage()
    {
        if (frames.Count == 0) throw new Exception("No frames to assemble into message");

        var data = frames.SelectMany(f => f.Payload).ToArray();
        return frames.First().OpCode switch
        {
            FrameOpCode.Text => new Message.Text(Encoding.UTF8.GetString(data)),
            FrameOpCode.Binary => new Message.Binary(data),
            _ => throw new Exception("Invalid Frame OpCode")
        };
    }
}