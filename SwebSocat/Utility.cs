
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

    public static class ReadLiner
    {
        private static TaskCompletionSource<string>? tcs = null;

        public static void Init() => Task.Run(ReadLineWorker);
        public static async Task<string?> ReadLineAsync(CancellationToken token)
        {
            var tcs = new TaskCompletionSource<string>();
            ReadLiner.tcs = tcs;
            using var registration = token.Register(() => tcs.TrySetCanceled());
            try
            {
                return await tcs.Task;
            }
            catch
            {
                ReadLiner.tcs = null;
                return null;
            }
        }

        private static async Task ReadLineWorker()
        {
            while (true)
            {
                while (!Console.KeyAvailable) await Task.Delay(100);
                var line = Console.ReadLine();
                if (line == null) return;
                if (tcs is TaskCompletionSource<string> t)
                {
                    tcs = null;
                    t.TrySetResult(line);
                }
            }
        }
    }
}