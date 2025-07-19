using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpStream.Server;
using CSharpStream.Client;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run [server|client]");
            return;
        }

        var mode = args[0].ToLowerInvariant();

        if (mode == "server")
        {
            using var cts = new CancellationTokenSource();
            var server = new Server(8000, cts.Token); // pass port and token

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            await server.StartAsync();
        }
        else if (mode == "client")
        {
            var client = new Client();
            await client.RunAsync("127.0.0.1", 8000);
        }
        else
        {
            Console.WriteLine("Unknown mode. Use 'server' or 'client'.");
        }
    }
}