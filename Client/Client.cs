using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CSharpStream.Models;

namespace CSharpStream.Client
{
    public class Client
    {
        public async Task RunAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            Console.Write("Enter your name: ");
            var user = new User
            {
                Name = Console.ReadLine()?.Trim()
            };
            Logger.Info("Client", $"Username: {user.Name}");
            if (string.IsNullOrEmpty(user.Name))
                user.Name = "Anonymous";

            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken);
            Logger.Info("Client", $"Client connected: {host}:{port}");
            await using var stream = client.GetStream();

            var receiveTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                while (!cancellationToken.IsCancellationRequested)
                {
                    // ReSharper disable once AccessToDisposedClosure
                    if (stream == null) continue;
                    var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server disconnected.");
                        Logger.Info("Server", "Server disconnected.");
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Message? message;

                    try
                    {
                        message = JsonSerializer.Deserialize<Message>(json);
                    }
                    catch (JsonException)
                    {
                        Console.WriteLine("Received malformed message.");
                        Logger.Error("Client",$"Received malformed message");
                        continue;
                    }

                    if (message != null)
                        Console.WriteLine($"[{message.Timestamp:T}] {message.Sender}: {message.Content}");
                }
            }, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var message = new Message(input, user.Name);
                var json = JsonSerializer.Serialize(message) + "\n";
                var bytes = Encoding.UTF8.GetBytes(json);

                try
                {
                    await stream.WriteAsync(bytes, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Send error: {ex.Message}");
                    break;
                }
            }

            await receiveTask;
        }
    }
}
