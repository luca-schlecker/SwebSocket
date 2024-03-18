using System.Text;
using SwebSocket;

namespace SwebSocat;

class Utility
{
    public static Task<string?> ReadLineAsync(CancellationToken cancellation)
    {
        return Task.Run(() =>
        {
            while (!Console.KeyAvailable)
            {
                if (cancellation.IsCancellationRequested)
                    return null;

                Thread.Sleep(100);
            }
            return Console.ReadLine();
        });
    }

    public static void PrintMessage(Message message)
    {
        if (message is Message.Text text)
            Console.WriteLine(text.Data);
        else if (message is Message.Binary binary)
        {
            StringBuilder sb = new();
            foreach (byte b in binary.Data)
                sb.Append($"0x{b:X2} ");
            Console.WriteLine(sb.ToString().Trim());
        }
        else throw new ArgumentException();
    }
}