
namespace SwebSocket;

public abstract class Message
{
    // Prevents external classes from inheriting from this class
    internal Message() { }
}

public class TextMessage : Message
{
    public string Text { get; }
    public TextMessage(string text) => Text = text;
}

public class BinaryMessage : Message
{
    public byte[] Data { get; }
    public BinaryMessage(byte[] data) => Data = data;
}