
using System.Text;
using SwebSocket;

namespace SwebSocat;

static class Utility
{
    public static void Print(this Message message)
    {
        if (message is TextMessage text)
            Console.WriteLine(text.Text.Trim());
        else if (message is BinaryMessage binary)
        {
            StringBuilder sb = new();
            foreach (byte b in binary.Data)
                sb.Append($"0x{b:X2} ");
            Console.WriteLine(sb.ToString().Trim());
        }
        else throw new ArgumentException();
    }

    public static Task<string?> ReadLineAsync(CancellationToken cancellation)
    {
        return Task.Run(async () =>
        {
            while (!Console.KeyAvailable)
            {
                if (cancellation.IsCancellationRequested)
                    return null;

                await Task.Delay(100);
            }
            return Console.ReadLine();
        });
    }
}