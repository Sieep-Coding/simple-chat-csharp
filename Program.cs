using CSharpStream.Models;

namespace CSharpStream;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Logger.Init("debug.txt");

        if (args.Length == 0)
        {
            Logger.Warn("Program", "Usage: dotnet run [server|client]");
            return;
        }

        var mode = args[0].ToLowerInvariant();

        switch (mode)
        {
            case "server":
            {
                var server = new CSharpStream.Server.Server(8000, CancellationToken.None);
                Logger.Info("Program", "Starting server...");
                await server.StartAsync();
                break;
            }
            case "client":
            {
                var client = new CSharpStream.Client.Client();
                Logger.Info("Program", "Starting client...");
                await client.RunAsync("127.0.0.1", 8000);
                break;
            }
            default:
                Logger.Warn("Program", "Unknown mode. Use 'server' or 'client'.");
                break;
        }

        Logger.Shutdown();
    }
}